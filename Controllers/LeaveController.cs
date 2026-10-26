using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HostelEase.Data;
using HostelEase.Models;
using HostelEase.Services;
using HostelEase.ViewModels;

namespace HostelEase.Controllers
{
    [Authorize(Roles = "Admin,Warden")]
    public class LeaveController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeaveController(
            ApplicationDbContext context,
            INotificationService notificationService,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _notificationService = notificationService;
            _auditService = auditService;
            _userManager = userManager;
        }

        // GET: /Leave
        public async Task<IActionResult> Index(string? status, string? search)
        {
            var query = _context.LeaveRequests
                .Include(l => l.Student)
                    .ThenInclude(s => s!.Allocations.Where(a => a.Status == "Active"))
                        .ThenInclude(a => a.Bed)
                            .ThenInclude(b => b!.Room)
                                .ThenInclude(r => r!.Hostel)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(l => l.Status == status);
            }
            else
            {
                // Default to Pending first
                query = query.Where(l => l.Status == "Pending");
                status = "Pending";
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(l => l.Student!.FullName.ToLower().Contains(s) ||
                                         l.Student.StudentId.ToLower().Contains(s) ||
                                         l.Destination.ToLower().Contains(s) ||
                                         l.Reason.ToLower().Contains(s));
            }

            var leaves = await query.OrderByDescending(l => l.SubmissionDate).ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.Search = search;

            return View(leaves);
        }

        // GET: /Leave/Review/{id}
        public async Task<IActionResult> Review(int id)
        {
            var leave = await _context.LeaveRequests
                .Include(l => l.Student)
                    .ThenInclude(s => s!.Allocations.Where(a => a.Status == "Active"))
                        .ThenInclude(a => a.Bed)
                            .ThenInclude(b => b!.Room)
                                .ThenInclude(r => r!.Hostel)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leave == null) return NotFound();

            var activeAlloc = leave.Student?.Allocations.FirstOrDefault();
            var roomInfo = activeAlloc != null
                ? $"{activeAlloc.Bed?.Room?.Hostel?.Name} - Room {activeAlloc.Bed?.Room?.RoomNumber}"
                : "No active room";

            var vm = new LeaveReviewViewModel
            {
                Id = leave.Id,
                StudentName = leave.Student?.FullName ?? "",
                StudentIdString = leave.Student?.StudentId ?? "",
                RoomDetails = roomInfo,
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                Reason = leave.Reason,
                Destination = leave.Destination,
                EmergencyContact = leave.EmergencyContact,
                ParentInformed = leave.ParentInformed,
                Status = leave.Status == "Pending" ? "Approved" : leave.Status,
                ReviewRemarks = leave.ReviewRemarks ?? ""
            };

            return View(vm);
        }

        // POST: /Leave/Review
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(LeaveReviewViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var leave = await _context.LeaveRequests
                .Include(l => l.Student)
                    .ThenInclude(s => s!.User)
                .FirstOrDefaultAsync(l => l.Id == model.Id);

            if (leave == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            leave.Status = model.Status;
            leave.ReviewRemarks = model.ReviewRemarks.Trim();
            leave.ReviewedByUserId = userId;
            leave.ReviewDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify Student
            if (leave.Student?.User != null)
            {
                var notifType = model.Status == "Approved" ? "Success" : "Danger";
                await _notificationService.CreateNotificationAsync(
                    leave.Student.UserId,
                    $"Leave Request {model.Status}",
                    $"Your leave request from {leave.StartDate:dd MMM} to {leave.EndDate:dd MMM} has been {model.Status}. Remarks: {leave.ReviewRemarks}",
                    "/Student/MyLeaveRequests",
                    notifType);
            }

            await _auditService.LogAsync("ReviewLeave", "LeaveRequest", leave.Id.ToString(), $"Leave request #{leave.Id} {model.Status} by staff", userId);

            TempData["SuccessMessage"] = $"Leave request has been {model.Status}.";
            return RedirectToAction("Index");
        }
    }
}
