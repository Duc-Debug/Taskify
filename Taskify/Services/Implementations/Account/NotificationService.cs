using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;


namespace Taskify.Services
{
    public class NotificationService: INotificationService
    {
        private readonly AppDbContext _context;
        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public Task CreateInfoNotificationAsync(Guid userId, string message)
        {
           var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Message = message,
                Type = NotificationType.Info,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            return _context.SaveChangesAsync(); 
        }

        public async Task CreateInviteNotificationAsync(Guid fromUserId, Guid toUserId, Guid teamId, string teamName)
        {
          var sender = await _context.Users.FindAsync(fromUserId);
            var senderName = sender != null ? sender.FullName : "Someone";
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = toUserId,
                Message = $"{senderName} has invited you to join the team '{teamName}'.",
                Type = NotificationType.TeamInvite,
                SenderId = fromUserId,
                ReferenceId = teamId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<NotificationViewModel>> GetNotificationsAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new NotificationViewModel
                {
                    Id = n.Id,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();
        }
    }
}
