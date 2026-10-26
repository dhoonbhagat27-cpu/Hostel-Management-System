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
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var totalBeds = await _context.Beds.CountAsync();
            var occupiedBeds = await _context.Beds.CountAsync(b => b.Status == "Occupied");
            var availableBeds = await _context.Beds.CountAsync(b => b.Status == "Available");
            var occupancyRate = totalBeds > 0 ? Math.Round(((double)occupiedBeds / totalBeds) * 100, 1) : 0;

            var totalCollected = await _context.FeePayments.SumAsync(p => (decimal?)p.Amount) ?? 0.00m;
            var outstanding = await _context.StudentFees.SumAsync(f => (decimal?)f.BalanceAmount) ?? 0.00m;

            var vm = new AdminDashboardViewModel
            {
                TotalStudents = await _context.Students.CountAsync(),
                ActiveStudents = await _context.Students.CountAsync(s => s.IsActive),
                TotalHostels = await _context.Hostels.CountAsync(h => h.Status == "Active"),
                TotalRooms = await _context.Rooms.CountAsync(r => r.Status != "Inactive"),
                TotalBeds = totalBeds,
                OccupiedBeds = occupiedBeds,
                AvailableBeds = availableBeds,
                OccupancyPercentage = occupancyRate,
                TotalFeeCollection = totalCollected,
                OutstandingFees = outstanding,
                PendingComplaints = await _context.Complaints.CountAsync(c => c.Status == "Submitted" || c.Status == "In Progress"),
                PendingLeaveRequests = await _context.LeaveRequests.CountAsync(l => l.Status == "Pending"),
                VisitorsInside = await _context.Visitors.CountAsync(v => v.ExitTime == null),

                RecentRegistrations = await _context.Students
                    .OrderByDescending(s => s.AdmissionDate)
                    .Take(5)
                    .ToListAsync(),

                RecentPayments = await _context.FeePayments
                    .Include(p => p.StudentFee)
                        .ThenInclude(f => f!.Student)
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(5)
                    .ToListAsync(),

                RecentComplaints = await _context.Complaints
                    .Include(c => c.Student)
                    .OrderByDescending(c => c.SubmissionDate)
                    .Take(5)
                    .ToListAsync(),

                PendingApprovals = await _context.LeaveRequests
                    .Include(l => l.Student)
                    .Where(l => l.Status == "Pending")
                    .OrderBy(l => l.StartDate)
                    .Take(5)
                    .ToListAsync(),

                RecentVisitors = await _context.Visitors
                    .Include(v => v.Student)
                    .OrderByDescending(v => v.EntryTime)
                    .Take(5)
                    .ToListAsync()
            };

            // Calculate Monthly Fee Collection for Chart (Last 6 months)
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
            var payments = await _context.FeePayments
                .Where(p => p.PaymentDate >= new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1))
                .ToListAsync();

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = DateTime.UtcNow.AddMonths(-i);
                var monthName = monthDate.ToString("MMM yyyy");
                var sum = payments
                    .Where(p => p.PaymentDate.Year == monthDate.Year && p.PaymentDate.Month == monthDate.Month)
                    .Sum(p => p.Amount);

                vm.MonthlyFeeLabels.Add(monthName);
                vm.MonthlyFeeValues.Add(sum);
            }

            // Hostel Occupancy Chart
            var hostels = await _context.Hostels
                .Include(h => h.Rooms)
                    .ThenInclude(r => r.Beds)
                .ToListAsync();

            foreach (var h in hostels)
            {
                var allBeds = h.Rooms.SelectMany(r => r.Beds).ToList();
                vm.HostelOccupancyLabels.Add(h.Name);
                vm.HostelOccupiedValues.Add(allBeds.Count(b => b.Status == "Occupied"));
                vm.HostelCapacityValues.Add(allBeds.Count);
            }

            // Complaints Breakdown
            vm.ComplaintSubmittedCount = await _context.Complaints.CountAsync(c => c.Status == "Submitted");
            vm.ComplaintInProgressCount = await _context.Complaints.CountAsync(c => c.Status == "In Progress");
            vm.ComplaintResolvedCount = await _context.Complaints.CountAsync(c => c.Status == "Resolved" || c.Status == "Closed");

            // Leave Requests Breakdown
            vm.LeavePendingCount = await _context.LeaveRequests.CountAsync(l => l.Status == "Pending");
            vm.LeaveApprovedCount = await _context.LeaveRequests.CountAsync(l => l.Status == "Approved");
            vm.LeaveRejectedCount = await _context.LeaveRequests.CountAsync(l => l.Status == "Rejected");

            return View(vm);
        }

        // GET: /Admin/Staff
        public async Task<IActionResult> Staff()
        {
            var staffList = await _context.StaffMembers
                .Include(s => s.AssignedHostel)
                .Include(s => s.User)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            return View(staffList);
        }

        // GET: /Admin/CreateStaff
        public async Task<IActionResult> CreateStaff()
        {
            ViewBag.Hostels = new SelectList(await _context.Hostels.Where(h => h.Status == "Active").ToListAsync(), "Id", "Name");
            return View(new StaffCreateViewModel());
        }

        // POST: /Admin/CreateStaff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(StaffCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Hostels = new SelectList(await _context.Hostels.Where(h => h.Status == "Active").ToListAsync(), "Id", "Name");
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                ViewBag.Hostels = new SelectList(await _context.Hostels.Where(h => h.Status == "Active").ToListAsync(), "Id", "Name");
                return View(model);
            }

            var existingStaffId = await _context.StaffMembers.AnyAsync(s => s.StaffId == model.StaffId);
            if (existingStaffId)
            {
                ModelState.AddModelError("StaffId", "This Staff ID already exists.");
                ViewBag.Hostels = new SelectList(await _context.Hostels.Where(h => h.Status == "Active").ToListAsync(), "Id", "Name");
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var role = model.Designation.Contains("Admin") ? "Admin" : "Warden";

                var user = new ApplicationUser
                {
                    UserName = model.Email.Trim(),
                    Email = model.Email.Trim(),
                    FullName = model.FullName.Trim(),
                    PhoneNumber = model.PhoneNumber.Trim(),
                    UserType = role,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createRes = await _userManager.CreateAsync(user, model.Password);
                if (!createRes.Succeeded)
                {
                    foreach (var err in createRes.Errors)
                    {
                        ModelState.AddModelError(string.Empty, err.Description);
                    }
                    ViewBag.Hostels = new SelectList(await _context.Hostels.Where(h => h.Status == "Active").ToListAsync(), "Id", "Name");
                    return View(model);
                }

                await _userManager.AddToRoleAsync(user, role);

                var staff = new Staff
                {
                    UserId = user.Id,
                    StaffId = model.StaffId.Trim(),
                    FullName = model.FullName.Trim(),
                    Email = model.Email.Trim(),
                    PhoneNumber = model.PhoneNumber.Trim(),
                    Designation = model.Designation,
                    AssignedHostelId = model.AssignedHostelId,
                    JoiningDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.StaffMembers.Add(staff);
                await _context.SaveChangesAsync();

                if (model.AssignedHostelId.HasValue)
                {
                    var hostel = await _context.Hostels.FindAsync(model.AssignedHostelId.Value);
                    if (hostel != null && hostel.AssignedWardenId == null)
                    {
                        hostel.AssignedWardenId = staff.Id;
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();

                await _auditService.LogAsync("Create", "Staff", staff.Id.ToString(), $"Staff account {staff.FullName} ({role}) created.", _userManager.GetUserId(User));
                TempData["SuccessMessage"] = $"Staff member '{staff.FullName}' created successfully with {role} privileges.";
                return RedirectToAction("Staff");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
                ViewBag.Hostels = new SelectList(await _context.Hostels.Where(h => h.Status == "Active").ToListAsync(), "Id", "Name");
                return View(model);
            }
        }

        // POST: /Admin/ToggleStaffStatus/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStaffStatus(int id)
        {
            var staff = await _context.StaffMembers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (staff != null)
            {
                staff.IsActive = !staff.IsActive;
                if (staff.User != null)
                {
                    staff.User.IsActive = staff.IsActive;
                }
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("ToggleStatus", "Staff", staff.Id.ToString(), $"Staff status toggled to {(staff.IsActive ? "Active" : "Inactive")}", _userManager.GetUserId(User));
                TempData["SuccessMessage"] = $"Staff {staff.FullName} is now {(staff.IsActive ? "Active" : "Inactive")}.";
            }

            return RedirectToAction("Staff");
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(users);
        }

        // POST: /Admin/ToggleUserStatus/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var currentUserId = _userManager.GetUserId(User);
                if (user.Id == currentUserId)
                {
                    TempData["ErrorMessage"] = "You cannot deactivate your own administrative account.";
                    return RedirectToAction("Users");
                }

                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);

                await _auditService.LogAsync("ToggleStatus", "ApplicationUser", user.Id, $"User account {user.Email} is now {(user.IsActive ? "Active" : "Deactivated")}", currentUserId);
                TempData["SuccessMessage"] = $"User {user.Email} status updated to {(user.IsActive ? "Active" : "Deactivated")}.";
            }

            return RedirectToAction("Users");
        }

        // GET: /Admin/AuditLogs
        public async Task<IActionResult> AuditLogs(string? search = null, string? actionType = null, int page = 1)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a => (a.UserEmail != null && a.UserEmail.Contains(search)) ||
                                         a.Details.Contains(search) ||
                                         a.EntityName.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(actionType))
            {
                query = query.Where(a => a.Action == actionType);
            }

            var totalItems = await query.CountAsync();
            var pageSize = 25;
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.ActionType = actionType;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(logs);
        }

        // GET: /Admin/Settings
        public IActionResult Settings()
        {
            return View();
        }
    }
}
