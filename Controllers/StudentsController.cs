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
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly IAllocationService _allocationService;
        private readonly INotificationService _notificationService;

        public StudentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService,
            IAllocationService allocationService,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
            _allocationService = allocationService;
            _notificationService = notificationService;
        }

        // GET: /Students
        public async Task<IActionResult> Index(string? searchTerm, string? department, string? academicYear, string? status, int pageNumber = 1)
        {
            var query = _context.Students
                .Include(s => s.Allocations.Where(a => a.Status == "Active"))
                    .ThenInclude(a => a.Bed)
                        .ThenInclude(b => b!.Room)
                            .ThenInclude(r => r!.Hostel)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim().ToLower();
                query = query.Where(s => s.FullName.ToLower().Contains(search) ||
                                         s.StudentId.ToLower().Contains(search) ||
                                         s.Email.ToLower().Contains(search) ||
                                         s.PhoneNumber.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(department) && department != "All")
            {
                query = query.Where(s => s.Department == department);
            }

            if (!string.IsNullOrWhiteSpace(academicYear) && academicYear != "All")
            {
                query = query.Where(s => s.AcademicYear == academicYear);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                bool isActive = status == "Active";
                query = query.Where(s => s.IsActive == isActive);
            }

            var totalItems = await query.CountAsync();
            var pageSize = 10;
            var students = await query
                .OrderBy(s => s.FullName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new StudentListFilterViewModel
            {
                SearchTerm = searchTerm,
                Department = department,
                AcademicYear = academicYear,
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                Students = students
            };

            return View(vm);
        }

        // GET: /Students/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Allocations)
                    .ThenInclude(a => a.Bed)
                        .ThenInclude(b => b!.Room)
                            .ThenInclude(r => r!.Hostel)
                .Include(s => s.Fees)
                    .ThenInclude(f => f.Payments)
                .Include(s => s.Complaints)
                .Include(s => s.LeaveRequests)
                .Include(s => s.Visitors)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null) return NotFound();

            return View(student);
        }

        // GET: /Students/Create (Register new student name directly)
        [Authorize(Roles = "Admin,Warden")]
        public async Task<IActionResult> Create()
        {
            var vm = new StudentCreateViewModel();
            await PopulateAvailableBedsAsync(vm);
            return View(vm);
        }

        // POST: /Students/Create
        [HttpPost]
        [Authorize(Roles = "Admin,Warden")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateAvailableBedsAsync(model);
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                await PopulateAvailableBedsAsync(model);
                return View(model);
            }

            // Auto-generate Student ID if left blank
            var studentId = model.StudentId?.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(studentId))
            {
                var total = await _context.Students.CountAsync();
                studentId = $"GH2026-{(total + 1):D3}";
            }
            else
            {
                var existingStudentId = await _context.Students.AnyAsync(s => s.StudentId.ToLower() == studentId.ToLower());
                if (existingStudentId)
                {
                    ModelState.AddModelError("StudentId", "This Student ID / Enrollment No. is already registered.");
                    await PopulateAvailableBedsAsync(model);
                    return View(model);
                }
            }

            var password = string.IsNullOrWhiteSpace(model.Password) ? "Student@123" : model.Password.Trim();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email.Trim(),
                    Email = model.Email.Trim(),
                    FullName = model.FullName.Trim(),
                    PhoneNumber = model.PhoneNumber.Trim(),
                    UserType = "Student",
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createRes = await _userManager.CreateAsync(user, password);
                if (!createRes.Succeeded)
                {
                    foreach (var err in createRes.Errors)
                    {
                        ModelState.AddModelError(string.Empty, err.Description);
                    }
                    await PopulateAvailableBedsAsync(model);
                    return View(model);
                }

                await _userManager.AddToRoleAsync(user, "Student");

                var student = new Student
                {
                    UserId = user.Id,
                    StudentId = studentId,
                    FullName = model.FullName.Trim(),
                    Email = model.Email.Trim(),
                    PhoneNumber = model.PhoneNumber.Trim(),
                    Department = model.Department,
                    AcademicYear = model.AcademicYear,
                    DateOfBirth = model.DateOfBirth,
                    Gender = "Female",
                    GuardianName = model.GuardianName.Trim(),
                    GuardianContact = model.GuardianContact.Trim(),
                    LocalGuardianName = model.LocalGuardianName?.Trim(),
                    LocalGuardianContact = model.LocalGuardianContact?.Trim(),
                    LocalGuardianAddress = model.LocalGuardianAddress?.Trim(),
                    BloodGroup = model.BloodGroup?.Trim(),
                    EmergencyContact = model.EmergencyContact?.Trim() ?? model.GuardianContact.Trim(),
                    Address = model.Address.Trim(),
                    AdmissionDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Direct Bed Allocation if selected
                if (model.SelectedBedId.HasValue && model.SelectedBedId.Value > 0)
                {
                    var allocRes = await _allocationService.AllocateBedAsync(
                        student.Id, 
                        model.SelectedBedId.Value, 
                        DateTime.UtcNow, 
                        DateTime.UtcNow.AddMonths(12), 
                        "Allocated during resident registration", 
                        _userManager.GetUserId(User)!);

                    if (!allocRes.Success)
                    {
                        TempData["WarningMessage"] = $"Student was registered, but bed allocation note: {allocRes.Message}";
                    }
                }

                await _auditService.LogAsync("Create", "Student", student.Id.ToString(), $"Staff registered student {student.FullName} ({student.StudentId})", _userManager.GetUserId(User));
                TempData["SuccessMessage"] = $"Resident '{student.FullName}' (ID: {student.StudentId}) has been registered successfully.";
                return RedirectToAction("Details", new { id = student.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, $"Error registering student: {ex.Message}");
                await PopulateAvailableBedsAsync(model);
                return View(model);
            }
        }

        private async Task PopulateAvailableBedsAsync(StudentCreateViewModel model)
        {
            var beds = await _context.Beds
                .Include(b => b.Room)
                    .ThenInclude(r => r!.Hostel)
                .Where(b => b.Status == "Available")
                .OrderBy(b => b.Room!.Hostel!.Name)
                .ThenBy(b => b.Room!.RoomNumber)
                .ThenBy(b => b.BedNumber)
                .ToListAsync();

            model.AvailableBedsList = beds.Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = b.Id.ToString(),
                Text = $"{b.Room?.Hostel?.Name} - Room {b.Room?.RoomNumber} - Bed {b.BedNumber}"
            }).ToList();
        }

        // GET: /Students/Edit/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            var vm = new StudentEditViewModel
            {
                Id = student.Id,
                StudentId = student.StudentId,
                FullName = student.FullName,
                Email = student.Email,
                PhoneNumber = student.PhoneNumber,
                Department = student.Department,
                AcademicYear = student.AcademicYear,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                GuardianName = student.GuardianName,
                GuardianContact = student.GuardianContact,
                Address = student.Address,
                IsActive = student.IsActive
            };

            return View(vm);
        }

        // POST: /Students/Edit/{id}
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StudentEditViewModel model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null) return NotFound();

            // Check duplicate email
            var emailExists = await _context.Students.AnyAsync(s => s.Email == model.Email && s.Id != id);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Another student is already using this email.");
                return View(model);
            }

            student.FullName = model.FullName.Trim();
            student.Email = model.Email.Trim();
            student.PhoneNumber = model.PhoneNumber.Trim();
            student.Department = model.Department;
            student.AcademicYear = model.AcademicYear;
            student.DateOfBirth = model.DateOfBirth;
            student.Gender = model.Gender;
            student.GuardianName = model.GuardianName.Trim();
            student.GuardianContact = model.GuardianContact.Trim();
            student.Address = model.Address.Trim();
            student.IsActive = model.IsActive;

            if (student.User != null)
            {
                student.User.FullName = student.FullName;
                student.User.PhoneNumber = student.PhoneNumber;
                student.User.IsActive = student.IsActive;
            }

            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Update", "Student", student.Id.ToString(), $"Updated student profile {student.FullName}", _userManager.GetUserId(User));

            TempData["SuccessMessage"] = "Student details updated successfully.";
            return RedirectToAction("Details", new { id = student.Id });
        }

        // POST: /Students/ToggleStatus/{id}
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student != null)
            {
                student.IsActive = !student.IsActive;
                if (student.User != null)
                {
                    student.User.IsActive = student.IsActive;
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Student {student.FullName} is now {(student.IsActive ? "Active" : "Inactive")}.";
            }

            return RedirectToAction("Details", new { id });
        }
    }
}
