using HostelEase.Data;
using HostelEase.Models;

namespace HostelEase.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string action, string entityName, string? entityId, string details, string? userId = null, string? userEmail = null, string? ipAddress = null)
        {
            try
            {
                var audit = new AuditLog
                {
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    Details = details,
                    UserId = userId,
                    UserEmail = userEmail,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Never allow audit logging failure to crash primary business transactions
            }
        }
    }
}
