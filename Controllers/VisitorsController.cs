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
    public class VisitorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public VisitorsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        // GET: /Visitors
        public async Task<IActionResult> Index(bool? currentlyInside, string? search)
        {
            var query = _context.Visitors
                .Include(v => v.Student)
                    .ThenInclude(s => s!.Allocations.Where(a => a.Status == "Active"))
                        .ThenInclude(a => a.Bed)
                            .ThenInclude(b => b!.Room)
                                .ThenInclude(r => r!.Hostel)
                .AsQueryable();

            if (currentlyInside == true)
            {
                query = query.Where(v => v.ExitTime == null);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(v => v.VisitorName.ToLower().Contains(s) ||
                                         v.ContactNumber.Contains(s) ||
                                         v.Student!.FullName.ToLower().Contains(s) ||
                                         v.Student.StudentId.ToLower().Contains(s));
            }

            var visitors = await query.OrderByDescending(v => v.EntryTime).ToListAsync();

            ViewBag.CurrentlyInside = currentlyInside ?? false;
            ViewBag.Search = search;
            ViewBag.InsideCount = await _context.Visitors.CountAsync(v => v.ExitTime == null);

            return View(visitors);
        }

        // GET: /Visitors/Register
        public async Task<IActionResult> Register(int? studentId = null)
        {
            var students = await _context.Students
                .Where(s => s.IsActive)
                .OrderBy(s => s.FullName)
                .Select(s => new { s.Id, Display = $"{s.FullName} ({s.StudentId}) - {s.Department}" })
                .ToListAsync();

            var vm = new VisitorRegisterViewModel
            {
                StudentId = studentId ?? 0,
                StudentsList = new SelectList(students, "Id", "Display", studentId)
            };

            return View(vm);
        }

        // POST: /Visitors/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(VisitorRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var students = await _context.Students
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.FullName)
                    .Select(s => new { s.Id, Display = $"{s.FullName} ({s.StudentId}) - {s.Department}" })
                    .ToListAsync();

                model.StudentsList = new SelectList(students, "Id", "Display", model.StudentId);
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            var visitor = new Visitor
            {
                StudentId = model.StudentId,
                VisitorName = model.VisitorName.Trim(),
                ContactNumber = model.ContactNumber.Trim(),
                RelationToStudent = model.RelationToStudent,
                PurposeOfVisit = model.PurposeOfVisit.Trim(),
                EntryTime = DateTime.UtcNow,
                RecordedByUserId = userId ?? "System",
                Remarks = model.Remarks?.Trim()
            };

            _context.Visitors.Add(visitor);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("RegisterVisitor", "Visitor", visitor.Id.ToString(), $"Visitor {visitor.VisitorName} registered entry for student ID {visitor.StudentId}", userId);

            TempData["SuccessMessage"] = $"Visitor '{visitor.VisitorName}' check-in entry recorded successfully.";
            return RedirectToAction("Index", new { currentlyInside = true });
        }

        // POST: /Visitors/RecordExit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordExit(int id)
        {
            var visitor = await _context.Visitors.FindAsync(id);
            if (visitor == null) return NotFound();

            if (visitor.ExitTime != null)
            {
                TempData["InfoMessage"] = "Visitor exit has already been recorded.";
                return RedirectToAction("Index");
            }

            visitor.ExitTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("ExitVisitor", "Visitor", visitor.Id.ToString(), $"Visitor {visitor.VisitorName} exit recorded at {visitor.ExitTime:t}", _userManager.GetUserId(User));

            TempData["SuccessMessage"] = $"Exit recorded for visitor '{visitor.VisitorName}'.";
            return RedirectToAction("Index");
        }
    }
}
