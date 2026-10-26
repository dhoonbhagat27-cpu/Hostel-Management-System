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
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAllocationService _allocationService;
        private readonly IAuditService _auditService;

        public RoomsController(ApplicationDbContext context, IAllocationService allocationService, IAuditService auditService)
        {
            _context = context;
            _allocationService = allocationService;
            _auditService = auditService;
        }

        // GET: /Rooms
        public async Task<IActionResult> Index(int? hostelId, string? roomType, string? status)
        {
            var query = _context.Rooms
                .Include(r => r.Hostel)
                .Include(r => r.Beds)
                    .ThenInclude(b => b.Allocations.Where(a => a.Status == "Active"))
                        .ThenInclude(a => a.Student)
                .AsQueryable();

            if (hostelId.HasValue && hostelId.Value > 0)
            {
                query = query.Where(r => r.HostelId == hostelId.Value);
            }

            if (!string.IsNullOrWhiteSpace(roomType) && roomType != "All")
            {
                query = query.Where(r => r.RoomType == roomType);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(r => r.Status == status);
            }

            var rooms = await query.OrderBy(r => r.Hostel!.Name).ThenBy(r => r.RoomNumber).ToListAsync();

            ViewBag.Hostels = new SelectList(await _context.Hostels.ToListAsync(), "Id", "Name", hostelId);
            ViewBag.SelectedHostelId = hostelId;
            ViewBag.SelectedRoomType = roomType;
            ViewBag.SelectedStatus = status;

            return View(rooms);
        }

        // GET: /Rooms/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.Hostel)
                .Include(r => r.Beds)
                    .ThenInclude(b => b.Allocations.Where(a => a.Status == "Active"))
                        .ThenInclude(a => a.Student)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null) return NotFound();

            return View(room);
        }

        // GET: /Rooms/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int? hostelId)
        {
            ViewBag.Hostels = new SelectList(await _context.Hostels.Where(h => h.Status == "Active").ToListAsync(), "Id", "Name", hostelId);
            return View(new Room { HostelId = hostelId ?? 0, Capacity = 2, MonthlyFee = 5000.00m });
        }

        // POST: /Rooms/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room room, bool autoCreateBeds = true)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Hostels = new SelectList(await _context.Hostels.Where(h => h.Status == "Active").ToListAsync(), "Id", "Name", room.HostelId);
                return View(room);
            }

            var exists = await _context.Rooms.AnyAsync(r => r.HostelId == room.HostelId && r.RoomNumber == room.RoomNumber);
            if (exists)
            {
                ModelState.AddModelError("RoomNumber", "A room with this number already exists in the selected hostel.");
                ViewBag.Hostels = new SelectList(await _context.Hostels.Where(h => h.Status == "Active").ToListAsync(), "Id", "Name", room.HostelId);
                return View(room);
            }

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            // Auto-generate beds for capacity if requested
            if (autoCreateBeds && room.Capacity > 0)
            {
                char[] suffixes = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H' };
                for (int i = 0; i < room.Capacity; i++)
                {
                    var suffix = i < suffixes.Length ? suffixes[i].ToString() : (i + 1).ToString();
                    var bed = new Bed
                    {
                        RoomId = room.Id,
                        BedNumber = $"{room.RoomNumber}-{suffix}",
                        Status = "Available"
                    };
                    _context.Beds.Add(bed);
                }
                await _context.SaveChangesAsync();
            }

            await _auditService.LogAsync("Create", "Room", room.Id.ToString(), $"Created room {room.RoomNumber} with {room.Capacity} beds.");
            TempData["SuccessMessage"] = $"Room '{room.RoomNumber}' and beds created successfully.";
            return RedirectToAction("Details", new { id = room.Id });
        }

        // GET: /Rooms/Edit/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            ViewBag.Hostels = new SelectList(await _context.Hostels.ToListAsync(), "Id", "Name", room.HostelId);
            return View(room);
        }

        // POST: /Rooms/Edit/{id}
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Room room)
        {
            if (id != room.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                ViewBag.Hostels = new SelectList(await _context.Hostels.ToListAsync(), "Id", "Name", room.HostelId);
                return View(room);
            }

            var exists = await _context.Rooms.AnyAsync(r => r.HostelId == room.HostelId && r.RoomNumber == room.RoomNumber && r.Id != id);
            if (exists)
            {
                ModelState.AddModelError("RoomNumber", "Room number already exists in this hostel.");
                ViewBag.Hostels = new SelectList(await _context.Hostels.ToListAsync(), "Id", "Name", room.HostelId);
                return View(room);
            }

            _context.Update(room);
            await _context.SaveChangesAsync();

            await _allocationService.SyncRoomStatusAsync(room.Id);
            await _auditService.LogAsync("Update", "Room", room.Id.ToString(), $"Updated room {room.RoomNumber}.");

            TempData["SuccessMessage"] = "Room details updated.";
            return RedirectToAction("Details", new { id = room.Id });
        }
    }
}
