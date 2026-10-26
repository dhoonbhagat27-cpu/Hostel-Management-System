using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HostelEase.Data;
using HostelEase.Models;
using HostelEase.Services;

namespace HostelEase.Controllers
{
    [Authorize(Roles = "Admin,Warden")]
    public class AnnouncementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public AnnouncementsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        // GET: /Announcements
        public async Task<IActionResult> Index()
        {
            var announcements = await _context.Announcements
                .Include(a => a.Hostel)
                .OrderByDescending(a => a.PublishDate)
                .ToListAsync();

            return View(announcements);
        }

        // GET: /Announcements/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Hostels = new SelectList(await _context.Hostels.Where(h => h.Status == "Active").ToListAsync(), "Id", "Name");
            return View(new Announcement());
        }

        // POST: /Announcements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement announcement)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Hostels = new SelectList(await _context.Hostels.Where(h => h.Status == "Active").ToListAsync(), "Id", "Name", announcement.HostelId);
                return View(announcement);
            }

            var userId = _userManager.GetUserId(User);
            announcement.PublishedByUserId = userId ?? "System";
            announcement.PublishDate = DateTime.UtcNow;
            announcement.IsActive = true;

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("CreateAnnouncement", "Announcement", announcement.Id.ToString(), $"Notice '{announcement.Title}' published.", userId);

            TempData["SuccessMessage"] = "Announcement published successfully.";
            return RedirectToAction("Index");
        }

        // POST: /Announcements/Toggle/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var ann = await _context.Announcements.FindAsync(id);
            if (ann != null)
            {
                ann.IsActive = !ann.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Announcement is now {(ann.IsActive ? "Active" : "Archived")}.";
            }
            return RedirectToAction("Index");
        }

        // POST: /Announcements/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ann = await _context.Announcements.FindAsync(id);
            if (ann != null)
            {
                _context.Announcements.Remove(ann);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Announcement deleted.";
            }
            return RedirectToAction("Index");
        }
    }
}
