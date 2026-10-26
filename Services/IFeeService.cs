using HostelEase.Models;

namespace HostelEase.Services
{
    public interface IFeeService
    {
        Task<(bool Success, string Message, StudentFee? Fee)> AssignFeeToStudentAsync(
            int studentId, int feeStructureId, DateTime dueDate);

        Task<(bool Success, string Message, StudentFee? Fee)> CreateCustomFeeAsync(
            int studentId, string title, decimal amount, DateTime dueDate);

        Task<(bool Success, string Message, FeePayment? Payment)> RecordPaymentAsync(
            int studentFeeId, decimal amount, DateTime paymentDate, string paymentMethod, string transactionReference, string recordedByUserId, string? remarks);

        Task RefreshFeeStatusAsync(int studentFeeId);
    }
}
