using Taskify.Models;

namespace Taskify.Services
{
    public interface INotificationService
    {
        Task<List<NotificationViewModel>> GetNotificationsAsync(Guid userId);
    }
}
