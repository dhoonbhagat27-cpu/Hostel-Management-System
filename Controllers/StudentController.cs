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
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;

        public StudentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _auditService = auditService;
        }

        private async Task<Student?> GetCurrentStudentAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;

            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Allocations.Where(a => a.Status == "Active"))
                    .ThenInclude(a => a.Bed)
                        .ThenInclude(b => b!.Room)
                            .ThenInclude(r => r!.Hostel)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        // GET: /Student/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null)
            {
                TempData["ErrorMessage"] = "Student profile not found. Please contact administration.";
                return RedirectToAction("Login", "Account");
            }

            var fees = await _context.StudentFees
                .Where(f => f.StudentId == student.Id)
                .ToListAsync();

            var totalFee = fees.Sum(f => f.TotalAmount);
            var paidFee = fees.Sum(f => f.PaidAmount);
            var balanceFee = fees.Sum(f => f.BalanceAmount);

            var activeAllocation = student.Allocations.FirstOrDefault();

            var vm = new StudentDashboardViewModel
            {
                Student = student,
                CurrentAllocation = activeAllocation,
                TotalFee = totalFee,
                PaidFee = paidFee,
                BalanceFee = balanceFee,
                PendingComplaintsCount = await _context.Complaints.CountAsync(c => c.StudentId == student.Id && (c.Status == "Submitted" || c.Status == "In Progress")),
                ActiveLeavesCount = await _context.LeaveRequests.CountAsync(l => l.StudentId == student.Id && l.Status == "Approved" && l.EndDate >= DateTime.UtcNow),
                UnreadNotificationsCount = await _notificationService.GetUnreadCountAsync(student.UserId),
                PendingFees = fees.Where(f => f.BalanceAmount > 0).ToList(),
                RecentComplaints = await _context.Complaints
                    .Where(c => c.StudentId == student.Id)
                    .OrderByDescending(c => c.SubmissionDate)
                    .Take(4)
                    .ToListAsync(),
                RecentLeaves = await _context.LeaveRequests
                    .Where(l => l.StudentId == student.Id)
                    .OrderByDescending(l => l.SubmissionDate)
                    .Take(4)
                    .ToListAsync(),
                Announcements = await _context.Announcements
                    .Where(a => a.IsActive && (a.TargetAudience == "All" || a.TargetAudience == "Students" || 
                                (activeAllocation != null && a.HostelId == activeAllocation.Bed!.Room!.HostelId)))
                    .OrderByDescending(a => a.PublishDate)
                    .Take(4)
                    .ToListAsync()
            };

            return View(vm);
        }

        // GET: /Student/MyRoom
        public async Task<IActionResult> MyRoom()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToAction("Login", "Account");

            var activeAlloc = student.Allocations.FirstOrDefault();
            if (activeAlloc?.Bed?.Room == null)
            {
                ViewBag.HasRoom = false;
                return View();
            }

            ViewBag.HasRoom = true;
            ViewBag.Allocation = activeAlloc;
            ViewBag.Room = activeAlloc.Bed.Room;
            ViewBag.Hostel = activeAlloc.Bed.Room.Hostel;
            ViewBag.Bed = activeAlloc.Bed;

            // Retrieve roommates in the same room (Active allocations for other beds in same room)
            var roommates = await _context.RoomAllocations
                .Include(ra => ra.Student)
                .Include(ra => ra.Bed)
                .Where(ra => ra.Bed!.RoomId == activeAlloc.Bed.RoomId && ra.StudentId != student.Id && ra.Status == "Active")
                .ToListAsync();

            ViewBag.Roommates = roommates;
            return View();
        }

        // GET: /Student/MyFees
        public async Task<IActionResult> MyFees()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToAction("Login", "Account");

            var fees = await _context.StudentFees
                .Include(f => f.Payments)
                .Where(f => f.StudentId == student.Id)
                .OrderByDescending(f => f.DueDate)
                .ToListAsync();

            return View(fees);
        }

        // GET: /Student/PaymentHistory
        public async Task<IActionResult> PaymentHistory()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToAction("Login", "Account");

            var payments = await _context.FeePayments
                .Include(p => p.StudentFee)
                .Where(p => p.StudentFee!.StudentId == student.Id)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }

        // GET: /Student/MyComplaints
        public async Task<IActionResult> MyComplaints()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToAction("Login", "Account");

            var complaints = await _context.Complaints
                .Include(c => c.AssignedStaff)
                .Where(c => c.StudentId == student.Id)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            return View(complaints);
        }

        // GET: /Student/SubmitComplaint
        public IActionResult SubmitComplaint()
        {
            return View(new ComplaintCreateViewModel());
        }

        // POST: /Student/SubmitComplaint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitComplaint(ComplaintCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToAction("Login", "Account");

            var complaint = new Complaint
            {
                StudentId = student.Id,
                Category = model.Category,
                Priority = model.Priority,
                Subject = model.Subject.Trim(),
                Description = model.Description.Trim(),
                Status = "Submitted",
                SubmissionDate = DateTime.UtcNow
            };

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            // Notify hostel warden/admin
            var activeAlloc = student.Allocations.FirstOrDefault();
            int? hostelId = activeAlloc?.Bed?.Room?.HostelId;

            await _notificationService.NotifyAdminsAndWardensAsync(
                "New Complaint Submitted",
                $"Student {student.FullName} filed a {complaint.Priority} priority complaint regarding {complaint.Category}: '{complaint.Subject}'",
                "/Complaints/Index",
                "Warning",
                hostelId);

            await _auditService.LogAsync(
                "SubmitComplaint",
                "Complaint",
                complaint.Id.ToString(),
                $"Complaint filed by {student.FullName}: {complaint.Subject}",
                student.UserId);

            TempData["SuccessMessage"] = "Your complaint has been submitted successfully. The hostel warden will review it shortly.";
            return RedirectToAction("MyComplaints");
        }

        // GET: /Student/MyLeaveRequests
        public async Task<IActionResult> MyLeaveRequests()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToAction("Login", "Account");

            var leaves = await _context.LeaveRequests
                .Where(l => l.StudentId == student.Id)
                .OrderByDescending(l => l.SubmissionDate)
                .ToListAsync();

            return View(leaves);
        }

        // GET: /Student/ApplyLeave
        public IActionResult ApplyLeave()
        {
            return View(new LeaveApplyViewModel());
        }

        // POST: /Student/ApplyLeave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyLeave(LeaveApplyViewModel model)
        {
            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date cannot be earlier than start date.");
            }

            if (model.StartDate < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("StartDate", "Start date cannot be in the past.");
            }

            if (!ModelState.IsValid) return View(model);

            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToAction("Login", "Account");

            var leave = new LeaveRequest
            {
                StudentId = student.Id,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Reason = model.Reason.Trim(),
                Destination = model.Destination.Trim(),
                EmergencyContact = model.EmergencyContact.Trim(),
                ParentInformed = model.ParentInformed,
                Status = "Pending",
                SubmissionDate = DateTime.UtcNow
            };

            _context.LeaveRequests.Add(leave);
            await _context.SaveChangesAsync();

            var activeAlloc = student.Allocations.FirstOrDefault();
            int? hostelId = activeAlloc?.Bed?.Room?.HostelId;

            await _notificationService.NotifyAdminsAndWardensAsync(
                "New Leave Application",
                $"Student {student.FullName} applied for leave from {leave.StartDate:dd MMM} to {leave.EndDate:dd MMM} ({leave.Destination}).",
                "/Leave/Index",
                "Info",
                hostelId);

            await _auditService.LogAsync(
                "ApplyLeave",
                "LeaveRequest",
                leave.Id.ToString(),
                $"Leave applied by {student.FullName} ({leave.StartDate:d} to {leave.EndDate:d})",
                student.UserId);

            TempData["SuccessMessage"] = "Leave application submitted successfully. You will be notified once reviewed by your warden.";
            return RedirectToAction("MyLeaveRequests");
        }

        // POST: /Student/CancelLeave/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelLeave(int id)
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToAction("Login", "Account");

            var leave = await _context.LeaveRequests
                .FirstOrDefaultAsync(l => l.Id == id && l.StudentId == student.Id);

            if (leave != null && leave.Status == "Pending")
            {
                leave.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Leave request cancelled.";
            }
            else
            {
                TempData["ErrorMessage"] = "Only pending leave requests can be cancelled.";
            }

            return RedirectToAction("MyLeaveRequests");
        }

        // GET: /Student/Announcements
        public async Task<IActionResult> Announcements()
        {
            var student = await GetCurrentStudentAsync();
            var activeAlloc = student?.Allocations.FirstOrDefault();
            int? hostelId = activeAlloc?.Bed?.Room?.HostelId;

            var announcements = await _context.Announcements
                .Include(a => a.Hostel)
                .Where(a => a.IsActive && (a.TargetAudience == "All" || a.TargetAudience == "Students" || 
                            (hostelId.HasValue && a.HostelId == hostelId.Value)))
                .OrderByDescending(a => a.PublishDate)
                .ToListAsync();

            return View(announcements);
        }
    }
}
