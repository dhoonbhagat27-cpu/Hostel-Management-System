using Microsoft.EntityFrameworkCore;
using HostelEase.Data;
using HostelEase.Models;

namespace HostelEase.Services
{
    public class AllocationService : IAllocationService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;

        public AllocationService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IAuditService auditService)
        {
            _context = context;
            _notificationService = notificationService;
            _auditService = auditService;
        }

        public async Task<(bool Success, string Message, RoomAllocation? Allocation)> AllocateBedAsync(
            int studentId, int bedId, DateTime allocationDate, DateTime? expectedVacatingDate, string? remarks, string? allocatedByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null)
                    return (false, "Student record not found.", null);

                if (!student.IsActive)
                    return (false, "Cannot allocate bed to an inactive student.", null);

                // Check if student already has an active allocation
                var existingStudentAllocation = await _context.RoomAllocations
                    .FirstOrDefaultAsync(ra => ra.StudentId == studentId && ra.Status == "Active");

                if (existingStudentAllocation != null)
                    return (false, "This student already has an active bed allocation. Please transfer or vacate first.", null);

                var bed = await _context.Beds
                    .Include(b => b.Room)
                        .ThenInclude(r => r!.Hostel)
                    .FirstOrDefaultAsync(b => b.Id == bedId);

                if (bed == null)
                    return (false, "Bed not found.", null);

                if (bed.Status != "Available")
                    return (false, $"Bed {bed.BedNumber} is currently {bed.Status} and cannot be allocated.", null);

                if (bed.Room == null || bed.Room.Status == "Maintenance" || bed.Room.Status == "Inactive")
                    return (false, "The room containing this bed is currently under maintenance or inactive.", null);

                // Check active allocations count for this room to ensure capacity is not exceeded
                var activeBedAllocationsInRoom = await _context.Beds
                    .Where(b => b.RoomId == bed.RoomId && b.Status == "Occupied")
                    .CountAsync();

                if (activeBedAllocationsInRoom >= bed.Room.Capacity)
                    return (false, $"Room {bed.Room.RoomNumber} capacity ({bed.Room.Capacity}) has been reached.", null);

                // Create allocation record
                var allocation = new RoomAllocation
                {
                    StudentId = studentId,
                    BedId = bedId,
                    AllocationDate = allocationDate,
                    ExpectedVacatingDate = expectedVacatingDate,
                    Status = "Active",
                    Remarks = remarks,
                    AllocatedByUserId = allocatedByUserId
                };

                _context.RoomAllocations.Add(allocation);

                // Update Bed status
                bed.Status = "Occupied";
                bed.RowVersion = Guid.NewGuid();

                await _context.SaveChangesAsync();

                // Recalculate and sync room status
                await SyncRoomStatusInternalAsync(bed.RoomId);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Send notification to student
                if (student.User != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        student.UserId,
                        "Room Allocated",
                        $"You have been allocated Bed {bed.BedNumber} in Room {bed.Room.RoomNumber}, {bed.Room.Hostel?.Name}.",
                        "/Student/MyRoom",
                        "Success");
                }

                await _auditService.LogAsync(
                    "Allocate",
                    "RoomAllocation",
                    allocation.Id.ToString(),
                    $"Bed {bed.BedNumber} allocated to Student {student.FullName} ({student.StudentId})",
                    allocatedByUserId);

                return (true, "Bed allocated successfully.", allocation);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return (false, "A concurrency conflict occurred. The bed may have just been allocated by another administrator. Please refresh.", null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Allocation failed: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> TransferBedAsync(
            int currentAllocationId, int newBedId, string? remarks, string? transferredByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var currentAllocation = await _context.RoomAllocations
                    .Include(ra => ra.Bed)
                        .ThenInclude(b => b!.Room)
                    .Include(ra => ra.Student)
                        .ThenInclude(s => s!.User)
                    .FirstOrDefaultAsync(ra => ra.Id == currentAllocationId && ra.Status == "Active");

                if (currentAllocation == null)
                    return (false, "Active allocation not found.");

                var targetBed = await _context.Beds
                    .Include(b => b.Room)
                        .ThenInclude(r => r!.Hostel)
                    .FirstOrDefaultAsync(b => b.Id == newBedId);

                if (targetBed == null)
                    return (false, "Target bed not found.");

                if (targetBed.Status != "Available")
                    return (false, $"Target Bed {targetBed.BedNumber} is currently {targetBed.Status}.");

                var oldBed = currentAllocation.Bed;
                var oldRoomId = oldBed?.RoomId;

                // Close current allocation as Transferred
                currentAllocation.Status = "Transferred";
                currentAllocation.ActualVacatingDate = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(remarks))
                {
                    currentAllocation.Remarks = (currentAllocation.Remarks + " | Transfer: " + remarks).Trim();
                }

                if (oldBed != null)
                {
                    oldBed.Status = "Available";
                    oldBed.RowVersion = Guid.NewGuid();
                }

                // Create new allocation
                var newAllocation = new RoomAllocation
                {
                    StudentId = currentAllocation.StudentId,
                    BedId = newBedId,
                    AllocationDate = DateTime.UtcNow,
                    ExpectedVacatingDate = currentAllocation.ExpectedVacatingDate,
                    Status = "Active",
                    Remarks = $"Transferred from Bed {oldBed?.BedNumber}. {remarks}",
                    AllocatedByUserId = transferredByUserId
                };

                _context.RoomAllocations.Add(newAllocation);

                targetBed.Status = "Occupied";
                targetBed.RowVersion = Guid.NewGuid();

                await _context.SaveChangesAsync();

                if (oldRoomId.HasValue)
                {
                    await SyncRoomStatusInternalAsync(oldRoomId.Value);
                }
                await SyncRoomStatusInternalAsync(targetBed.RoomId);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (currentAllocation.Student?.User != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        currentAllocation.Student.UserId,
                        "Room Transfer Completed",
                        $"You have been transferred to Bed {targetBed.BedNumber} in Room {targetBed.Room?.RoomNumber}, {targetBed.Room?.Hostel?.Name}.",
                        "/Student/MyRoom",
                        "Info");
                }

                await _auditService.LogAsync(
                    "Transfer",
                    "RoomAllocation",
                    newAllocation.Id.ToString(),
                    $"Transferred Student {currentAllocation.Student?.FullName} from Bed {oldBed?.BedNumber} to Bed {targetBed.BedNumber}",
                    transferredByUserId);

                return (true, "Bed transferred successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Transfer failed: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> VacateBedAsync(
            int allocationId, DateTime actualVacatingDate, string? remarks, string? vacatedByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var allocation = await _context.RoomAllocations
                    .Include(ra => ra.Bed)
                        .ThenInclude(b => b!.Room)
                    .Include(ra => ra.Student)
                        .ThenInclude(s => s!.User)
                    .FirstOrDefaultAsync(ra => ra.Id == allocationId && ra.Status == "Active");

                if (allocation == null)
                    return (false, "Active allocation not found.");

                allocation.Status = "Vacated";
                allocation.ActualVacatingDate = actualVacatingDate;
                if (!string.IsNullOrWhiteSpace(remarks))
                {
                    allocation.Remarks = (allocation.Remarks + " | Vacate: " + remarks).Trim();
                }

                if (allocation.Bed != null)
                {
                    allocation.Bed.Status = "Available";
                    allocation.Bed.RowVersion = Guid.NewGuid();
                }

                await _context.SaveChangesAsync();

                if (allocation.Bed?.RoomId != null)
                {
                    await SyncRoomStatusInternalAsync(allocation.Bed.RoomId);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (allocation.Student?.User != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        allocation.Student.UserId,
                        "Bed Vacated",
                        $"Your check-out / vacating record for Bed {allocation.Bed?.BedNumber} has been recorded.",
                        "/Student/MyRoom",
                        "Warning");
                }

                await _auditService.LogAsync(
                    "Vacate",
                    "RoomAllocation",
                    allocation.Id.ToString(),
                    $"Vacated Bed {allocation.Bed?.BedNumber} for Student {allocation.Student?.FullName}",
                    vacatedByUserId);

                return (true, "Bed vacated successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Vacate operation failed: {ex.Message}");
            }
        }

        public async Task SyncRoomStatusAsync(int roomId)
        {
            await SyncRoomStatusInternalAsync(roomId);
            await _context.SaveChangesAsync();
        }

        private async Task SyncRoomStatusInternalAsync(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Beds)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || room.Status == "Maintenance" || room.Status == "Inactive")
                return;

            var totalBeds = room.Beds.Count;
            var occupiedBeds = room.Beds.Count(b => b.Status == "Occupied");

            if (occupiedBeds >= room.Capacity || (totalBeds > 0 && occupiedBeds >= totalBeds))
            {
                room.Status = "Full";
            }
            else
            {
                room.Status = "Available";
            }
        }
    }
}
