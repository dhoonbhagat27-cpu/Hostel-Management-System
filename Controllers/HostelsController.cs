using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HostelEase.Data;
using HostelEase.Models;
using HostelEase.Services;

namespace HostelEase.Controllers
{
    [Authorize(Roles = "Admin,Warden")]
    public class HostelsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public HostelsController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // GET: /Hostels
        public async Task<IActionResult> Index()
        {
            var hostels = await _context.Hostels
                .Include(h => h.AssignedWarden)
                .Include(h => h.Rooms)
                    .ThenInclude(r => r.Beds)
                .OrderBy(h => h.Name)
                .ToListAsync();

            return View(hostels);
        }

        // GET: /Hostels/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var hostel = await _context.Hostels
                .Include(h => h.AssignedWarden)
                .Include(h => h.Rooms)
                    .ThenInclude(r => r.Beds)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hostel == null) return NotFound();

            return View(hostel);
        }

        // GET: /Hostels/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Wardens = new SelectList(await _context.StaffMembers.Where(s => s.IsActive).ToListAsync(), "Id", "FullName");
            return View(new Hostel());
        }

        // POST: /Hostels/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Hostel hostel)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Wardens = new SelectList(await _context.StaffMembers.Where(s => s.IsActive).ToListAsync(), "Id", "FullName");
                return View(hostel);
            }

            var codeExists = await _context.Hostels.AnyAsync(h => h.Code == hostel.Code);
            if (codeExists)
            {
                ModelState.AddModelError("Code", "A hostel with this code already exists.");
                ViewBag.Wardens = new SelectList(await _context.StaffMembers.Where(s => s.IsActive).ToListAsync(), "Id", "FullName");
                return View(hostel);
            }

            _context.Hostels.Add(hostel);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Create", "Hostel", hostel.Id.ToString(), $"Hostel {hostel.Name} created.");
            TempData["SuccessMessage"] = $"Hostel '{hostel.Name}' created successfully.";
            return RedirectToAction("Index");
        }

        // GET: /Hostels/Edit/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var hostel = await _context.Hostels.FindAsync(id);
            if (hostel == null) return NotFound();

            ViewBag.Wardens = new SelectList(await _context.StaffMembers.Where(s => s.IsActive).ToListAsync(), "Id", "FullName", hostel.AssignedWardenId);
            return View(hostel);
        }

        // POST: /Hostels/Edit/{id}
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Hostel hostel)
        {
            if (id != hostel.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                ViewBag.Wardens = new SelectList(await _context.StaffMembers.Where(s => s.IsActive).ToListAsync(), "Id", "FullName", hostel.AssignedWardenId);
                return View(hostel);
            }

            var codeExists = await _context.Hostels.AnyAsync(h => h.Code == hostel.Code && h.Id != id);
            if (codeExists)
            {
                ModelState.AddModelError("Code", "Hostel code already in use.");
                ViewBag.Wardens = new SelectList(await _context.StaffMembers.Where(s => s.IsActive).ToListAsync(), "Id", "FullName", hostel.AssignedWardenId);
                return View(hostel);
            }

            _context.Update(hostel);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Update", "Hostel", hostel.Id.ToString(), $"Updated hostel {hostel.Name}");
            TempData["SuccessMessage"] = "Hostel details updated.";
            return RedirectToAction("Index");
        }
    }
}
