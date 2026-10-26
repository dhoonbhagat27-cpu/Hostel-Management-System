using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HostelEase.Data;
using HostelEase.Models;
using HostelEase.Services;

namespace HostelEase.Controllers
{
    [Authorize(Roles = "Admin,Warden")]
    public class BedsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAllocationService _allocationService;
        private readonly IAuditService _auditService;

        public BedsController(ApplicationDbContext context, IAllocationService allocationService, IAuditService auditService)
        {
            _context = context;
            _allocationService = allocationService;
            _auditService = auditService;
        }

        // POST: /Beds/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int roomId, string bedNumber)
        {
            if (string.IsNullOrWhiteSpace(bedNumber))
            {
                TempData["ErrorMessage"] = "Bed identifier is required.";
                return RedirectToAction("Details", "Rooms", new { id = roomId });
            }

            var room = await _context.Rooms.Include(r => r.Beds).FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null) return NotFound();

            if (room.Beds.Any(b => b.BedNumber.Equals(bedNumber.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                TempData["ErrorMessage"] = $"Bed {bedNumber} already exists in this room.";
                return RedirectToAction("Details", "Rooms", new { id = roomId });
            }

            var bed = new Bed
            {
                RoomId = roomId,
                BedNumber = bedNumber.Trim().ToUpper(),
                Status = "Available"
            };

            _context.Beds.Add(bed);
            await _context.SaveChangesAsync();

            await _allocationService.SyncRoomStatusAsync(roomId);
            await _auditService.LogAsync("Create", "Bed", bed.Id.ToString(), $"Added bed {bed.BedNumber} to room {room.RoomNumber}");

            TempData["SuccessMessage"] = $"Bed {bed.BedNumber} added successfully.";
            return RedirectToAction("Details", "Rooms", new { id = roomId });
        }

        // POST: /Beds/ToggleMaintenance/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMaintenance(int id)
        {
            var bed = await _context.Beds.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);
            if (bed == null) return NotFound();

            if (bed.Status == "Occupied")
            {
                TempData["ErrorMessage"] = "Cannot put an occupied bed under maintenance. Please transfer or vacate the student first.";
                return RedirectToAction("Details", "Rooms", new { id = bed.RoomId });
            }

            bed.Status = bed.Status == "Maintenance" ? "Available" : "Maintenance";
            await _context.SaveChangesAsync();

            await _allocationService.SyncRoomStatusAsync(bed.RoomId);
            await _auditService.LogAsync("StatusChange", "Bed", bed.Id.ToString(), $"Bed {bed.BedNumber} status changed to {bed.Status}");

            TempData["SuccessMessage"] = $"Bed {bed.BedNumber} status changed to {bed.Status}.";
            return RedirectToAction("Details", "Rooms", new { id = bed.RoomId });
        }
    }
}
