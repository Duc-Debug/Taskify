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
