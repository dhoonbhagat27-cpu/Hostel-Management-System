namespace HostelEase.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string title, string message, string? linkUrl = null, string type = "Info");
        Task NotifyAdminsAndWardensAsync(string title, string message, string? linkUrl = null, string type = "Info", int? specificHostelId = null);
        Task<int> GetUnreadCountAsync(string userId);
    }
}
