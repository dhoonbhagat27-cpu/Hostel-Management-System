using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HostelEase.Data;
using HostelEase.Models;
using HostelEase.Services;
using HostelEase.ViewModels;

namespace HostelEase.Controllers
{
    [Authorize(Roles = "Admin,Warden")]
    public class AllocationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAllocationService _allocationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AllocationsController(
            ApplicationDbContext context,
            IAllocationService allocationService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _allocationService = allocationService;
            _userManager = userManager;
        }

        // GET: /Allocations
        public async Task<IActionResult> Index(string? status, string? search, int? hostelId)
        {
            var query = _context.RoomAllocations
                .Include(ra => ra.Student)
                .Include(ra => ra.Bed)
                    .ThenInclude(b => b!.Room)
                        .ThenInclude(r => r!.Hostel)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(ra => ra.Status == status);
            }
            else
            {
                // Default to Active allocations first
                query = query.Where(ra => ra.Status == "Active");
                status = "Active";
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(ra => ra.Student!.FullName.ToLower().Contains(s) ||
                                         ra.Student.StudentId.ToLower().Contains(s) ||
                                         ra.Bed!.BedNumber.ToLower().Contains(s) ||
                                         ra.Bed.Room!.RoomNumber.ToLower().Contains(s));
            }

            if (hostelId.HasValue && hostelId.Value > 0)
            {
                query = query.Where(ra => ra.Bed!.Room!.HostelId == hostelId.Value);
            }

            var allocations = await query.OrderByDescending(ra => ra.AllocationDate).ToListAsync();

            ViewBag.Hostels = new SelectList(await _context.Hostels.ToListAsync(), "Id", "Name", hostelId);
            ViewBag.CurrentStatus = status;
            ViewBag.Search = search;
            ViewBag.HostelId = hostelId;

            return View(allocations);
        }

        // GET: /Allocations/Allocate
        public async Task<IActionResult> Allocate(int? studentId = null)
        {
            // Get students who DO NOT have an active allocation
            var allocatedStudentIds = await _context.RoomAllocations
                .Where(ra => ra.Status == "Active")
                .Select(ra => ra.StudentId)
                .ToListAsync();

            var eligibleStudents = await _context.Students
                .Where(s => s.IsActive && !allocatedStudentIds.Contains(s.Id))
                .OrderBy(s => s.FullName)
                .Select(s => new { s.Id, Display = $"{s.FullName} ({s.StudentId}) - {s.Department}" })
                .ToListAsync();

            var availableBeds = await _context.Beds
                .Include(b => b.Room)
                    .ThenInclude(r => r!.Hostel)
                .Where(b => b.Status == "Available" && b.Room!.Status != "Maintenance" && b.Room.Status != "Inactive")
                .OrderBy(b => b.Room!.Hostel!.Name)
                .ThenBy(b => b.Room!.RoomNumber)
                .ThenBy(b => b.BedNumber)
                .Select(b => new { b.Id, Display = $"{b.Room!.Hostel!.Name} - Room {b.Room.RoomNumber} [Bed {b.BedNumber}] ({b.Room.RoomType} - ₹{b.Room.MonthlyFee:N0}/mo)" })
                .ToListAsync();

            var vm = new AllocateBedViewModel
            {
                StudentId = studentId ?? 0,
                AllocationDate = DateTime.UtcNow,
                ExpectedVacatingDate = DateTime.UtcNow.AddMonths(10),
                StudentsList = new SelectList(eligibleStudents, "Id", "Display", studentId),
                AvailableBedsList = new SelectList(availableBeds, "Id", "Display")
            };

            return View(vm);
        }

        // POST: /Allocations/Allocate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Allocate(AllocateBedViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateAllocationDropdowns(model);
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            var result = await _allocationService.AllocateBedAsync(
                model.StudentId,
                model.BedId,
                model.AllocationDate,
                model.ExpectedVacatingDate,
                model.Remarks,
                userId);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await PopulateAllocationDropdowns(model);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Index");
        }

        // GET: /Allocations/Transfer/{id}
        public async Task<IActionResult> Transfer(int id)
        {
            var allocation = await _context.RoomAllocations
                .Include(ra => ra.Student)
                .Include(ra => ra.Bed)
                    .ThenInclude(b => b!.Room)
                        .ThenInclude(r => r!.Hostel)
                .FirstOrDefaultAsync(ra => ra.Id == id && ra.Status == "Active");

            if (allocation == null)
            {
                TempData["ErrorMessage"] = "Active room allocation not found.";
                return RedirectToAction("Index");
            }

            var availableBeds = await _context.Beds
                .Include(b => b.Room)
                    .ThenInclude(r => r!.Hostel)
                .Where(b => b.Status == "Available" && b.Id != allocation.BedId)
                .OrderBy(b => b.Room!.Hostel!.Name)
                .ThenBy(b => b.Room!.RoomNumber)
                .Select(b => new { b.Id, Display = $"{b.Room!.Hostel!.Name} - Room {b.Room.RoomNumber} [Bed {b.BedNumber}] ({b.Room.RoomType})" })
                .ToListAsync();

            var vm = new TransferBedViewModel
            {
                CurrentAllocationId = allocation.Id,
                StudentName = allocation.Student?.FullName ?? "",
                StudentIdString = allocation.Student?.StudentId ?? "",
                CurrentBedNumber = allocation.Bed?.BedNumber ?? "",
                CurrentRoomNumber = allocation.Bed?.Room?.RoomNumber ?? "",
                CurrentHostelName = allocation.Bed?.Room?.Hostel?.Name ?? "",
                AvailableBedsList = new SelectList(availableBeds, "Id", "Display")
            };

            return View(vm);
        }

        // POST: /Allocations/Transfer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(TransferBedViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            var result = await _allocationService.TransferBedAsync(
                model.CurrentAllocationId,
                model.NewBedId,
                model.Remarks,
                userId);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Index");
        }

        // GET: /Allocations/Vacate/{id}
        public async Task<IActionResult> Vacate(int id)
        {
            var allocation = await _context.RoomAllocations
                .Include(ra => ra.Student)
                .Include(ra => ra.Bed)
                    .ThenInclude(b => b!.Room)
                        .ThenInclude(r => r!.Hostel)
                .FirstOrDefaultAsync(ra => ra.Id == id && ra.Status == "Active");

            if (allocation == null)
            {
                TempData["ErrorMessage"] = "Active allocation not found.";
                return RedirectToAction("Index");
            }

            var vm = new VacateBedViewModel
            {
                AllocationId = allocation.Id,
                StudentName = allocation.Student?.FullName ?? "",
                BedNumber = allocation.Bed?.BedNumber ?? "",
                RoomNumber = allocation.Bed?.Room?.RoomNumber ?? "",
                HostelName = allocation.Bed?.Room?.Hostel?.Name ?? "",
                AllocationDate = allocation.AllocationDate,
                ActualVacatingDate = DateTime.UtcNow
            };

            return View(vm);
        }

        // POST: /Allocations/Vacate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vacate(VacateBedViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = _userManager.GetUserId(User);
            var result = await _allocationService.VacateBedAsync(
                model.AllocationId,
                model.ActualVacatingDate,
                model.Remarks,
                userId);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Index");
        }

        private async Task PopulateAllocationDropdowns(AllocateBedViewModel model)
        {
            var allocatedStudentIds = await _context.RoomAllocations
                .Where(ra => ra.Status == "Active")
                .Select(ra => ra.StudentId)
                .ToListAsync();

            var eligibleStudents = await _context.Students
                .Where(s => s.IsActive && !allocatedStudentIds.Contains(s.Id))
                .OrderBy(s => s.FullName)
                .Select(s => new { s.Id, Display = $"{s.FullName} ({s.StudentId}) - {s.Department}" })
                .ToListAsync();

            var availableBeds = await _context.Beds
                .Include(b => b.Room)
                    .ThenInclude(r => r!.Hostel)
                .Where(b => b.Status == "Available")
                .Select(b => new { b.Id, Display = $"{b.Room!.Hostel!.Name} - Room {b.Room.RoomNumber} [Bed {b.BedNumber}] ({b.Room.RoomType})" })
                .ToListAsync();

            model.StudentsList = new SelectList(eligibleStudents, "Id", "Display", model.StudentId);
            model.AvailableBedsList = new SelectList(availableBeds, "Id", "Display", model.BedId);
        }
    }
}
