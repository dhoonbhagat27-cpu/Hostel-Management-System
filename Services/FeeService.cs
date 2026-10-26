using Microsoft.EntityFrameworkCore;
using HostelEase.Data;
using HostelEase.Models;

namespace HostelEase.Services
{
    public class FeeService : IFeeService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;

        public FeeService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IAuditService auditService)
        {
            _context = context;
            _notificationService = notificationService;
            _auditService = auditService;
        }

        public async Task<(bool Success, string Message, StudentFee? Fee)> AssignFeeToStudentAsync(
            int studentId, int feeStructureId, DateTime dueDate)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return (false, "Student not found.", null);

            var feeStructure = await _context.FeeStructures.FindAsync(feeStructureId);
            if (feeStructure == null || !feeStructure.IsActive)
                return (false, "Selected fee structure is not active or found.", null);

            var fee = new StudentFee
            {
                StudentId = studentId,
                FeeStructureId = feeStructureId,
                FeeTitle = $"{feeStructure.Title} ({feeStructure.AcademicYear} - {feeStructure.Term})",
                DueDate = dueDate,
                TotalAmount = feeStructure.TotalAmount,
                PaidAmount = 0.00m,
                BalanceAmount = feeStructure.TotalAmount,
                Status = "Unpaid",
                CreatedAt = DateTime.UtcNow
            };

            _context.StudentFees.Add(fee);
            await _context.SaveChangesAsync();

            if (student.User != null)
            {
                await _notificationService.CreateNotificationAsync(
                    student.UserId,
                    "New Fee Obligation Added",
                    $"Hostel fee '{fee.FeeTitle}' of ₹{fee.TotalAmount:N2} due on {fee.DueDate:dd MMM yyyy} has been added to your account.",
                    "/Student/MyFees",
                    "Warning");
            }

            return (true, "Fee successfully assigned to student.", fee);
        }

        public async Task<(bool Success, string Message, StudentFee? Fee)> CreateCustomFeeAsync(
            int studentId, string title, decimal amount, DateTime dueDate)
        {
            if (amount <= 0)
                return (false, "Amount must be greater than zero.", null);

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return (false, "Student not found.", null);

            var fee = new StudentFee
            {
                StudentId = studentId,
                FeeTitle = title,
                DueDate = dueDate,
                TotalAmount = amount,
                PaidAmount = 0.00m,
                BalanceAmount = amount,
                Status = "Unpaid",
                CreatedAt = DateTime.UtcNow
            };

            _context.StudentFees.Add(fee);
            await _context.SaveChangesAsync();

            if (student.User != null)
            {
                await _notificationService.CreateNotificationAsync(
                    student.UserId,
                    "New Fee Assigned",
                    $"Fee '{fee.FeeTitle}' of ₹{fee.TotalAmount:N2} due on {fee.DueDate:dd MMM yyyy} has been generated.",
                    "/Student/MyFees",
                    "Warning");
            }

            return (true, "Custom fee created successfully.", fee);
        }

        public async Task<(bool Success, string Message, FeePayment? Payment)> RecordPaymentAsync(
            int studentFeeId, decimal amount, DateTime paymentDate, string paymentMethod, string transactionReference, string recordedByUserId, string? remarks)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (amount <= 0)
                    return (false, "Payment amount must be greater than zero.", null);

                var fee = await _context.StudentFees
                    .Include(f => f.Student)
                        .ThenInclude(s => s!.User)
                    .FirstOrDefaultAsync(f => f.Id == studentFeeId);

                if (fee == null)
                    return (false, "Fee record not found.", null);

                if (fee.BalanceAmount <= 0)
                    return (false, "This fee has already been fully settled.", null);

                if (amount > fee.BalanceAmount)
                    return (false, $"Payment amount (₹{amount:N2}) exceeds outstanding balance (₹{fee.BalanceAmount:N2}).", null);

                // Generate unique receipt number
                var receiptNumber = $"REC-{DateTime.UtcNow:yyyyMM}-{new Random().Next(10000, 99999)}";
                while (await _context.FeePayments.AnyAsync(p => p.ReceiptNumber == receiptNumber))
                {
                    receiptNumber = $"REC-{DateTime.UtcNow:yyyyMM}-{new Random().Next(10000, 99999)}";
                }

                var payment = new FeePayment
                {
                    StudentFeeId = studentFeeId,
                    ReceiptNumber = receiptNumber,
                    Amount = amount,
                    PaymentDate = paymentDate,
                    PaymentMethod = paymentMethod,
                    TransactionReference = string.IsNullOrWhiteSpace(transactionReference) ? "OFFLINE-" + Guid.NewGuid().ToString("N")[..8].ToUpper() : transactionReference,
                    RecordedByUserId = recordedByUserId,
                    Remarks = remarks
                };

                _context.FeePayments.Add(payment);

                // Update Fee totals
                fee.PaidAmount += amount;
                fee.BalanceAmount = fee.TotalAmount - fee.PaidAmount;

                if (fee.BalanceAmount <= 0.00m)
                {
                    fee.Status = "Paid";
                }
                else
                {
                    fee.Status = "Partially Paid";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Notify student
                if (fee.Student?.User != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        fee.Student.UserId,
                        "Payment Receipt Issued",
                        $"Payment of ₹{amount:N2} received for '{fee.FeeTitle}'. Receipt #{receiptNumber}.",
                        $"/Student/PaymentHistory",
                        "Success");
                }

                await _auditService.LogAsync(
                    "Payment",
                    "FeePayment",
                    payment.Id.ToString(),
                    $"Recorded payment of ₹{amount:N2} (Receipt #{receiptNumber}) for Student {fee.Student?.FullName}",
                    recordedByUserId);

                return (true, "Payment recorded successfully.", payment);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Payment processing failed: {ex.Message}", null);
            }
        }

        public async Task RefreshFeeStatusAsync(int studentFeeId)
        {
            var fee = await _context.StudentFees
                .Include(f => f.Payments)
                .FirstOrDefaultAsync(f => f.Id == studentFeeId);

            if (fee == null) return;

            fee.PaidAmount = fee.Payments.Sum(p => p.Amount);
            fee.BalanceAmount = fee.TotalAmount - fee.PaidAmount;

            if (fee.BalanceAmount <= 0)
            {
                fee.Status = "Paid";
            }
            else if (fee.PaidAmount > 0)
            {
                fee.Status = "Partially Paid";
            }
            else if (DateTime.UtcNow > fee.DueDate)
            {
                fee.Status = "Overdue";
            }
            else
            {
                fee.Status = "Unpaid";
            }

            await _context.SaveChangesAsync();
        }
    }
}
