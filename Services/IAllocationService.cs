using HostelEase.Models;

namespace HostelEase.Services
{
    public interface IAllocationService
    {
        Task<(bool Success, string Message, RoomAllocation? Allocation)> AllocateBedAsync(
            int studentId, int bedId, DateTime allocationDate, DateTime? expectedVacatingDate, string? remarks, string? allocatedByUserId);

        Task<(bool Success, string Message)> TransferBedAsync(
            int currentAllocationId, int newBedId, string? remarks, string? transferredByUserId);

        Task<(bool Success, string Message)> VacateBedAsync(
            int allocationId, DateTime actualVacatingDate, string? remarks, string? vacatedByUserId);

        Task SyncRoomStatusAsync(int roomId);
    }
}
