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
    public class FeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeeService _feeService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FeesController(
            ApplicationDbContext context,
            IFeeService feeService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _feeService = feeService;
            _userManager = userManager;
        }

        // GET: /Fees
        public async Task<IActionResult> Index(string? status, string? search)
        {
            var query = _context.StudentFees
                .Include(f => f.Student)
                .Include(f => f.FeeStructure)
                .Include(f => f.Payments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(f => f.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(f => f.Student!.FullName.ToLower().Contains(s) ||
                                         f.Student.StudentId.ToLower().Contains(s) ||
                                         f.FeeTitle.ToLower().Contains(s));
            }

            var fees = await query.OrderByDescending(f => f.DueDate).ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.Search = search;
            ViewBag.TotalDue = fees.Sum(f => f.TotalAmount);
            ViewBag.TotalCollected = fees.Sum(f => f.PaidAmount);
            ViewBag.TotalOutstanding = fees.Sum(f => f.BalanceAmount);

            return View(fees);
        }

        // GET: /Fees/Structures
        public async Task<IActionResult> Structures()
        {
            var structures = await _context.FeeStructures.OrderBy(f => f.Title).ToListAsync();
            return View(structures);
        }

        // GET: /Fees/CreateStructure
        [Authorize(Roles = "Admin")]
        public IActionResult CreateStructure()
        {
            return View(new FeeStructure());
        }

        // POST: /Fees/CreateStructure
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStructure(FeeStructure structure)
        {
            structure.TotalAmount = structure.HostelRent + structure.MessFee + structure.MaintenanceFee + structure.SecurityDeposit;

            if (!ModelState.IsValid)
            {
                return View(structure);
            }

            _context.FeeStructures.Add(structure);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Fee structure '{structure.Title}' created.";
            return RedirectToAction("Structures");
        }

        // GET: /Fees/AssignFee
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignFee(int? studentId = null)
        {
            var students = await _context.Students
                .Where(s => s.IsActive)
                .OrderBy(s => s.FullName)
                .Select(s => new { s.Id, Display = $"{s.FullName} ({s.StudentId}) - {s.Department}" })
                .ToListAsync();

            var structures = await _context.FeeStructures
                .Where(f => f.IsActive)
                .OrderBy(f => f.Title)
                .Select(f => new { f.Id, Display = $"{f.Title} - ₹{f.TotalAmount:N2} ({f.Term})" })
                .ToListAsync();

            var vm = new AssignFeeViewModel
            {
                StudentId = studentId ?? 0,
                DueDate = DateTime.UtcNow.AddDays(30),
                StudentsList = new SelectList(students, "Id", "Display", studentId),
                FeeStructuresList = new SelectList(structures, "Id", "Display")
            };

            return View(vm);
        }

        // POST: /Fees/AssignFee
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignFee(AssignFeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateAssignFeeDropdowns(model);
                return View(model);
            }

            var result = await _feeService.AssignFeeToStudentAsync(model.StudentId, model.FeeStructureId, model.DueDate);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await PopulateAssignFeeDropdowns(model);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Index");
        }

        // GET: /Fees/RecordPayment/{id}
        public async Task<IActionResult> RecordPayment(int id)
        {
            var fee = await _context.StudentFees
                .Include(f => f.Student)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fee == null) return NotFound();

            if (fee.BalanceAmount <= 0)
            {
                TempData["InfoMessage"] = "This fee obligation has already been fully paid.";
                return RedirectToAction("Index");
            }

            var vm = new RecordPaymentViewModel
            {
                StudentFeeId = fee.Id,
                StudentName = fee.Student?.FullName ?? "",
                StudentIdString = fee.Student?.StudentId ?? "",
                FeeTitle = fee.FeeTitle,
                TotalAmount = fee.TotalAmount,
                AlreadyPaidAmount = fee.PaidAmount,
                BalanceAmount = fee.BalanceAmount,
                Amount = fee.BalanceAmount, // Pre-fill with remaining balance
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "UPI",
                TransactionReference = "TXN-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999)
            };

            return View(vm);
        }

        // POST: /Fees/RecordPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(RecordPaymentViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = _userManager.GetUserId(User);
            var result = await _feeService.RecordPaymentAsync(
                model.StudentFeeId,
                model.Amount,
                model.PaymentDate,
                model.PaymentMethod,
                model.TransactionReference,
                userId ?? "System",
                model.Remarks);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Payment recorded successfully! Receipt #{result.Payment?.ReceiptNumber}";
            return RedirectToAction("Receipt", new { id = result.Payment?.Id });
        }

        // GET: /Fees/Payments
        public async Task<IActionResult> Payments(string? search, string? method)
        {
            var query = _context.FeePayments
                .Include(p => p.StudentFee)
                    .ThenInclude(f => f!.Student)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.ReceiptNumber.ToLower().Contains(s) ||
                                         p.TransactionReference.ToLower().Contains(s) ||
                                         p.StudentFee!.Student!.FullName.ToLower().Contains(s) ||
                                         p.StudentFee.Student.StudentId.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(method) && method != "All")
            {
                query = query.Where(p => p.PaymentMethod == method);
            }

            var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();

            ViewBag.Search = search;
            ViewBag.Method = method;
            ViewBag.TotalAmount = payments.Sum(p => p.Amount);

            return View(payments);
        }

        // GET: /Fees/Receipt/{id}
        [AllowAnonymous]
        public async Task<IActionResult> Receipt(int id)
        {
            var payment = await _context.FeePayments
                .Include(p => p.StudentFee)
                    .ThenInclude(f => f!.Student)
                        .ThenInclude(s => s!.Allocations.Where(a => a.Status == "Active"))
                            .ThenInclude(a => a.Bed)
                                .ThenInclude(b => b!.Room)
                                    .ThenInclude(r => r!.Hostel)
                .Include(p => p.StudentFee)
                    .ThenInclude(f => f!.FeeStructure)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null) return NotFound();

            // Authorization: if student, must be their own payment
            if (User.IsInRole("Student"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (payment.StudentFee?.Student?.UserId != currentUserId)
                {
                    return Forbid();
                }
            }

            return View(payment);
        }

        private async Task PopulateAssignFeeDropdowns(AssignFeeViewModel model)
        {
            var students = await _context.Students
                .Where(s => s.IsActive)
                .OrderBy(s => s.FullName)
                .Select(s => new { s.Id, Display = $"{s.FullName} ({s.StudentId}) - {s.Department}" })
                .ToListAsync();

            var structures = await _context.FeeStructures
                .Where(f => f.IsActive)
                .OrderBy(f => f.Title)
                .Select(f => new { f.Id, Display = $"{f.Title} - ₹{f.TotalAmount:N2}" })
                .ToListAsync();

            model.StudentsList = new SelectList(students, "Id", "Display", model.StudentId);
            model.FeeStructuresList = new SelectList(structures, "Id", "Display", model.FeeStructureId);
        }
    }
}
