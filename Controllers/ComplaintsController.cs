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
    public class ComplaintsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ComplaintsController(
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

        // GET: /Complaints
        public async Task<IActionResult> Index(string? status, string? priority, string? category, string? search)
        {
            var query = _context.Complaints
                .Include(c => c.Student)
                    .ThenInclude(s => s!.Allocations.Where(a => a.Status == "Active"))
                        .ThenInclude(a => a.Bed)
                            .ThenInclude(b => b!.Room)
                                .ThenInclude(r => r!.Hostel)
                .Include(c => c.AssignedStaff)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(c => c.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority) && priority != "All")
            {
                query = query.Where(c => c.Priority == priority);
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                query = query.Where(c => c.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c => c.Subject.ToLower().Contains(s) ||
                                         c.Description.ToLower().Contains(s) ||
                                         c.Student!.FullName.ToLower().Contains(s) ||
                                         c.Student.StudentId.ToLower().Contains(s));
            }

            var complaints = await query.OrderByDescending(c => c.SubmissionDate).ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPriority = priority;
            ViewBag.CurrentCategory = category;
            ViewBag.Search = search;

            return View(complaints);
        }

        // GET: /Complaints/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var complaint = await _context.Complaints
                .Include(c => c.Student)
                    .ThenInclude(s => s!.Allocations.Where(a => a.Status == "Active"))
                        .ThenInclude(a => a.Bed)
                            .ThenInclude(b => b!.Room)
                                .ThenInclude(r => r!.Hostel)
                .Include(c => c.AssignedStaff)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (complaint == null) return NotFound();

            return View(complaint);
        }

        // GET: /Complaints/Resolve/{id}
        public async Task<IActionResult> Resolve(int id)
        {
            var complaint = await _context.Complaints
                .Include(c => c.Student)
                    .ThenInclude(s => s!.Allocations.Where(a => a.Status == "Active"))
                        .ThenInclude(a => a.Bed)
                            .ThenInclude(b => b!.Room)
                                .ThenInclude(r => r!.Hostel)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (complaint == null) return NotFound();

            var activeAlloc = complaint.Student?.Allocations.FirstOrDefault();
            var roomInfo = activeAlloc != null 
                ? $"{activeAlloc.Bed?.Room?.Hostel?.Name} - Room {activeAlloc.Bed?.Room?.RoomNumber} (Bed {activeAlloc.Bed?.BedNumber})"
                : "Not currently allocated";

            var staffList = await _context.StaffMembers
                .Where(s => s.IsActive)
                .OrderBy(s => s.FullName)
                .Select(s => new { s.Id, Display = $"{s.FullName} ({s.Designation})" })
                .ToListAsync();

            var vm = new ComplaintResolveViewModel
            {
                Id = complaint.Id,
                StudentName = complaint.Student?.FullName ?? "",
                RoomInfo = roomInfo,
                Category = complaint.Category,
                Priority = complaint.Priority,
                Subject = complaint.Subject,
                Description = complaint.Description,
                SubmissionDate = complaint.SubmissionDate,
                Status = complaint.Status == "Submitted" ? "In Progress" : complaint.Status,
                ResolutionRemarks = complaint.ResolutionRemarks ?? "",
                AssignedStaffId = complaint.AssignedStaffId,
                StaffList = new SelectList(staffList, "Id", "Display", complaint.AssignedStaffId)
            };

            return View(vm);
        }

        // POST: /Complaints/Resolve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(ComplaintResolveViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var staffList = await _context.StaffMembers.Where(s => s.IsActive).ToListAsync();
                model.StaffList = new SelectList(staffList, "Id", "FullName", model.AssignedStaffId);
                return View(model);
            }

            var complaint = await _context.Complaints
                .Include(c => c.Student)
                    .ThenInclude(s => s!.User)
                .FirstOrDefaultAsync(c => c.Id == model.Id);

            if (complaint == null) return NotFound();

            complaint.Status = model.Status;
            complaint.ResolutionRemarks = model.ResolutionRemarks.Trim();
            complaint.AssignedStaffId = model.AssignedStaffId;

            if (model.Status == "Resolved" || model.Status == "Closed")
            {
                complaint.ResolvedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Notify Student
            if (complaint.Student?.User != null)
            {
                await _notificationService.CreateNotificationAsync(
                    complaint.Student.UserId,
                    $"Complaint Status Updated: {complaint.Status}",
                    $"Your complaint '{complaint.Subject}' has been marked as {complaint.Status}. Remarks: {complaint.ResolutionRemarks}",
                    "/Student/MyComplaints",
                    model.Status == "Resolved" ? "Success" : "Info");
            }

            await _auditService.LogAsync("ResolveComplaint", "Complaint", complaint.Id.ToString(), $"Complaint #{complaint.Id} updated to {complaint.Status}", _userManager.GetUserId(User));

            TempData["SuccessMessage"] = $"Complaint status updated to '{complaint.Status}'.";
            return RedirectToAction("Index");
        }
    }
}
