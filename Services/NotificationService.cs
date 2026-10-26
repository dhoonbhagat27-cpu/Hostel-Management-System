using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HostelEase.Data;
using HostelEase.Models;

namespace HostelEase.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task CreateNotificationAsync(string userId, string title, string message, string? linkUrl = null, string type = "Info")
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                LinkUrl = linkUrl,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task NotifyAdminsAndWardensAsync(string title, string message, string? linkUrl = null, string type = "Info", int? specificHostelId = null)
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var wardenUsers = await _userManager.GetUsersInRoleAsync("Warden");

            var targetUserIds = adminUsers.Select(u => u.Id).ToHashSet();

            if (specificHostelId.HasValue)
            {
                var assignedWardens = await _context.StaffMembers
                    .Where(s => s.AssignedHostelId == specificHostelId.Value && s.IsActive)
                    .Select(s => s.UserId)
                    .ToListAsync();

                foreach (var id in assignedWardens)
                {
                    targetUserIds.Add(id);
                }
            }
            else
            {
                foreach (var warden in wardenUsers)
                {
                    targetUserIds.Add(warden.Id);
                }
            }

            foreach (var userId in targetUserIds)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    LinkUrl = linkUrl,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }
    }
}
