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
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IAuditService auditService,
            INotificationService notificationService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _auditService = auditService;
            _notificationService = notificationService;
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToDashboard();
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login credentials.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact hostel administration.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _auditService.LogAsync(
                    "Login",
                    "ApplicationUser",
                    user.Id,
                    $"User {user.Email} successfully logged in.",
                    user.Id,
                    user.Email,
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return await RedirectToRoleDashboardAsync(user);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "This account is temporarily locked out due to multiple failed login attempts. Please try again after 10 minutes.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // GET: /Account/Register (Students Only)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToDashboard();
            }

            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Explicit Server-Side Enforcement for Hostel Rules & Curfew Declaration
            if (!model.AgreeToPolicies && !model.AcceptHostelRules)
            {
                ModelState.AddModelError(nameof(model.AgreeToPolicies), "You must accept the Girls' Hostel Regulations & 8:30 PM Night Curfew Policy.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check duplicate email
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            // Check duplicate student ID
            var existingStudentId = await _context.Students.AnyAsync(s => s.StudentId.ToLower() == model.StudentId.Trim().ToLower());
            if (existingStudentId)
            {
                ModelState.AddModelError("StudentId", "This Student ID / Enrollment Number is already registered in the system.");
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email.Trim(),
                    Email = model.Email.Trim(),
                    FullName = model.FullName.Trim(),
                    PhoneNumber = model.PhoneNumber.Trim(),
                    UserType = "Student", // Strict: Public registration is ONLY Student role
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, model.Password);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                // Add to Student role
                await _userManager.AddToRoleAsync(user, "Student");

                // Create Student Profile
                var student = new Student
                {
                    UserId = user.Id,
                    StudentId = model.StudentId.Trim().ToUpper(),
                    FullName = model.FullName.Trim(),
                    Email = model.Email.Trim(),
                    PhoneNumber = model.PhoneNumber.Trim(),
                    Department = model.Department,
                    AcademicYear = model.AcademicYear,
                    DateOfBirth = model.DateOfBirth,
                    Gender = "Female", // Strictly Female for Girls' Hostel
                    GuardianName = model.GuardianName.Trim(),
                    GuardianContact = model.GuardianContact.Trim(),
                    LocalGuardianName = model.LocalGuardianName?.Trim(),
                    LocalGuardianContact = model.LocalGuardianContact?.Trim(),
                    LocalGuardianAddress = model.LocalGuardianAddress?.Trim(),
                    BloodGroup = model.BloodGroup?.Trim(),
                    EmergencyContact = model.EmergencyContact?.Trim(),
                    Address = model.Address.Trim(),
                    AdmissionDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Send Welcome Notification
                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    "Welcome to HostelEase Girls' Hostel!",
                    "Your female resident account has been registered successfully. You may now check room availability, review curfew guidelines, and view fee schedules.",
                    "/Student/Dashboard",
                    "Success");

                await _auditService.LogAsync(
                    "Register",
                    "Student",
                    student.Id.ToString(),
                    $"Student {student.FullName} ({student.StudentId}) registered self account.",
                    user.Id,
                    user.Email);

                // Auto sign in student
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["SuccessMessage"] = "Registration successful! Welcome to HostelEase.";
                return RedirectToAction("Dashboard", "Student");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, $"An unexpected error occurred during registration: {ex.Message}");
                return View(model);
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = _userManager.GetUserId(User);
            var userEmail = User.Identity?.Name;

            await _signInManager.SignOutAsync();

            if (!string.IsNullOrEmpty(userId))
            {
                await _auditService.LogAsync("Logout", "ApplicationUser", userId, $"User {userEmail} logged out.", userId, userEmail);
            }

            TempData["InfoMessage"] = "You have been successfully logged out.";
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/Profile
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? user.UserType;

            var vm = new ProfileViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = primaryRole
            };

            if (primaryRole == "Student")
            {
                var student = await _context.Students
                    .Include(s => s.Allocations.Where(a => a.Status == "Active"))
                        .ThenInclude(a => a.Bed)
                            .ThenInclude(b => b!.Room)
                                .ThenInclude(r => r!.Hostel)
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (student != null)
                {
                    vm.StudentId = student.StudentId;
                    vm.Department = student.Department;
                    vm.AcademicYear = student.AcademicYear;
                    vm.GuardianName = student.GuardianName;
                    vm.GuardianContact = student.GuardianContact;
                    vm.Address = student.Address;

                    var activeAlloc = student.Allocations.FirstOrDefault();
                    if (activeAlloc?.Bed?.Room != null)
                    {
                        vm.AllocatedRoom = $"Room {activeAlloc.Bed.Room.RoomNumber} ({activeAlloc.Bed.Room.Hostel?.Name})";
                        vm.AllocatedBed = $"Bed {activeAlloc.Bed.BedNumber}";
                    }
                }
            }
            else if (primaryRole == "Warden" || primaryRole == "Admin")
            {
                var staff = await _context.StaffMembers
                    .Include(s => s.AssignedHostel)
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (staff != null)
                {
                    vm.StaffId = staff.StaffId;
                    vm.Designation = staff.Designation;
                    vm.AssignedHostelName = staff.AssignedHostel?.Name ?? "All Hostels (Admin)";
                }
            }

            return View(vm);
        }

        // GET: /Account/ChangePassword
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Account/ChangePassword
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                await _auditService.LogAsync("ChangePassword", "ApplicationUser", user.Id, "User updated their password.", user.Id, user.Email);
                TempData["SuccessMessage"] = "Your password has been changed successfully.";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { token, email = model.Email }, Request.Scheme);
                
                // For demonstrative & offline purposes, provide direct reset link preview in TempData
                TempData["ResetLink"] = callbackUrl;
            }

            TempData["SuccessMessage"] = "If an account with that email exists, password reset instructions have been generated.";
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            if (token == null || email == null)
            {
                return RedirectToAction("Login");
            }

            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["SuccessMessage"] = "Your password has been reset successfully. Please log in.";
                return RedirectToAction("Login");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your password has been reset successfully. You can now log in with your new password.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task<IActionResult> RedirectToRoleDashboardAsync(ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            if (await _userManager.IsInRoleAsync(user, "Warden"))
            {
                return RedirectToAction("Dashboard", "Warden");
            }
            return RedirectToAction("Dashboard", "Student");
        }

        private IActionResult RedirectToDashboard()
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Dashboard", "Admin");
            if (User.IsInRole("Warden")) return RedirectToAction("Dashboard", "Warden");
            return RedirectToAction("Dashboard", "Student");
        }
    }
}
