namespace HostelEase.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string entityName, string? entityId, string details, string? userId = null, string? userEmail = null, string? ipAddress = null);
    }
}
