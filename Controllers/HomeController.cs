using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HostelEase.Data;
using HostelEase.Models;

namespace HostelEase.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // If already authenticated, redirect to user's dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin")) return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole("Warden")) return RedirectToAction("Dashboard", "Warden");
                if (User.IsInRole("Student")) return RedirectToAction("Dashboard", "Student");
            }

            // Real dynamic counts from database
            ViewBag.TotalHostels = await _context.Hostels.CountAsync(h => h.Status == "Active");
            ViewBag.TotalRooms = await _context.Rooms.CountAsync(r => r.Status != "Inactive");
            ViewBag.TotalBeds = await _context.Beds.CountAsync();
            ViewBag.ActiveStudents = await _context.Students.CountAsync(s => s.IsActive);

            var activeAnnouncements = await _context.Announcements
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.PublishDate)
                .Take(3)
                .ToListAsync();

            return View(activeAnnouncements);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Features()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("/Home/NotFound")]
        public new IActionResult NotFound()
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }
    }
}
