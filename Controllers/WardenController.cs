using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HostelEase.Data;
using HostelEase.Models;
using HostelEase.ViewModels;

namespace HostelEase.Controllers
{
    [Authorize(Roles = "Warden,Admin")]
    public class WardenController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WardenController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Warden/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var staff = await _context.StaffMembers
                .Include(s => s.AssignedHostel)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            var hostelId = staff?.AssignedHostelId;
            Hostel? hostel = null;

            if (hostelId.HasValue)
            {
                hostel = await _context.Hostels
                    .Include(h => h.Rooms)
                        .ThenInclude(r => r.Beds)
                    .FirstOrDefaultAsync(h => h.Id == hostelId.Value);
            }
            else
            {
                // Fallback for demo or admin accessing warden dashboard: pick first hostel
                hostel = await _context.Hostels
                    .Include(h => h.Rooms)
                        .ThenInclude(r => r.Beds)
                    .FirstOrDefaultAsync();
            }

            var vm = new WardenDashboardViewModel
            {
                AssignedHostel = hostel
            };

            if (hostel != null)
            {
                var beds = hostel.Rooms.SelectMany(r => r.Beds).ToList();
                vm.TotalRooms = hostel.Rooms.Count;
                vm.TotalBeds = beds.Count;
                vm.OccupiedBeds = beds.Count(b => b.Status == "Occupied");
                vm.AvailableBeds = beds.Count(b => b.Status == "Available");
                vm.OccupancyPercentage = vm.TotalBeds > 0 ? Math.Round(((double)vm.OccupiedBeds / vm.TotalBeds) * 100, 1) : 0;

                // Hostel students count
                vm.HostelStudentsCount = await _context.RoomAllocations
                    .Where(ra => ra.Bed!.Room!.HostelId == hostel.Id && ra.Status == "Active")
                    .CountAsync();

                // Complaints for this hostel or assigned to this staff
                vm.PendingComplaints = await _context.Complaints
                    .Where(c => (c.AssignedStaffId == (staff != null ? staff.Id : null) || 
                                 c.Student!.Allocations.Any(a => a.Bed!.Room!.HostelId == hostel.Id && a.Status == "Active")) &&
                                 c.Status == "Submitted")
                    .CountAsync();

                vm.InProgressComplaints = await _context.Complaints
                    .Where(c => (c.AssignedStaffId == (staff != null ? staff.Id : null) || 
                                 c.Student!.Allocations.Any(a => a.Bed!.Room!.HostelId == hostel.Id && a.Status == "Active")) &&
                                 c.Status == "In Progress")
                    .CountAsync();

                vm.RecentComplaints = await _context.Complaints
                    .Include(c => c.Student)
                    .Where(c => c.Student!.Allocations.Any(a => a.Bed!.Room!.HostelId == hostel.Id && a.Status == "Active"))
                    .OrderByDescending(c => c.SubmissionDate)
                    .Take(5)
                    .ToListAsync();

                // Pending leave requests for students in this hostel
                vm.PendingLeaves = await _context.LeaveRequests
                    .Where(l => l.Status == "Pending" &&
                                l.Student!.Allocations.Any(a => a.Bed!.Room!.HostelId == hostel.Id && a.Status == "Active"))
                    .CountAsync();

                vm.PendingLeaveRequests = await _context.LeaveRequests
                    .Include(l => l.Student)
                    .Where(l => l.Status == "Pending" &&
                                l.Student!.Allocations.Any(a => a.Bed!.Room!.HostelId == hostel.Id && a.Status == "Active"))
                    .OrderBy(l => l.StartDate)
                    .Take(5)
                    .ToListAsync();

                // Visitors inside
                vm.VisitorsInside = await _context.Visitors
                    .Where(v => v.ExitTime == null &&
                                v.Student!.Allocations.Any(a => a.Bed!.Room!.HostelId == hostel.Id && a.Status == "Active"))
                    .CountAsync();

                vm.CurrentVisitors = await _context.Visitors
                    .Include(v => v.Student)
                    .Where(v => v.ExitTime == null &&
                                v.Student!.Allocations.Any(a => a.Bed!.Room!.HostelId == hostel.Id && a.Status == "Active"))
                    .OrderByDescending(v => v.EntryTime)
                    .Take(5)
                    .ToListAsync();

                vm.RoomsList = hostel.Rooms.OrderBy(r => r.RoomNumber).ToList();
            }

            return View(vm);
        }

        // GET: /Warden/AssignedHostel
        public async Task<IActionResult> AssignedHostel()
        {
            var user = await _userManager.GetUserAsync(User);
            var staff = await _context.StaffMembers.FirstOrDefaultAsync(s => s.UserId == (user != null ? user.Id : ""));
            
            var hostelId = staff?.AssignedHostelId;
            var hostel = await _context.Hostels
                .Include(h => h.AssignedWarden)
                .Include(h => h.Rooms)
                    .ThenInclude(r => r.Beds)
                .FirstOrDefaultAsync(h => hostelId.HasValue ? h.Id == hostelId.Value : true);

            if (hostel == null)
            {
                TempData["ErrorMessage"] = "No hostel assigned to your profile. Please contact administrator.";
                return RedirectToAction("Dashboard");
            }

            return View(hostel);
        }

        // GET: /Warden/Students
        public async Task<IActionResult> Students()
        {
            var user = await _userManager.GetUserAsync(User);
            var staff = await _context.StaffMembers.FirstOrDefaultAsync(s => s.UserId == (user != null ? user.Id : ""));
            var hostelId = staff?.AssignedHostelId;

            var query = _context.Students
                .Include(s => s.Allocations.Where(a => a.Status == "Active"))
                    .ThenInclude(a => a.Bed)
                        .ThenInclude(b => b!.Room)
                .AsQueryable();

            if (hostelId.HasValue)
            {
                query = query.Where(s => s.Allocations.Any(a => a.Bed!.Room!.HostelId == hostelId.Value && a.Status == "Active"));
            }

            var students = await query.OrderBy(s => s.FullName).ToListAsync();
            return View(students);
        }

        // GET: /Warden/RoomAvailability
        public async Task<IActionResult> RoomAvailability()
        {
            var user = await _userManager.GetUserAsync(User);
            var staff = await _context.StaffMembers.FirstOrDefaultAsync(s => s.UserId == (user != null ? user.Id : ""));
            var hostelId = staff?.AssignedHostelId;

            var query = _context.Rooms
                .Include(r => r.Hostel)
                .Include(r => r.Beds)
                    .ThenInclude(b => b.Allocations.Where(a => a.Status == "Active"))
                        .ThenInclude(a => a.Student)
                .AsQueryable();

            if (hostelId.HasValue)
            {
                query = query.Where(r => r.HostelId == hostelId.Value);
            }

            var rooms = await query.OrderBy(r => r.RoomNumber).ToListAsync();
            return View(rooms);
        }
    }
}
