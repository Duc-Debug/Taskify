using Taskify.Models;

namespace Taskify.Services
{
    public interface INotificationService
    {
        Task<List<NotificationViewModel>> GetNotificationsAsync(Guid userId);
        Task CreateInviteNotificationAsync(Guid fromUserId, Guid toUserId, Guid teamId, string teamName);
        Task CreateInfoNotificationAsync(Guid userId, string message);
        Task<bool> MarkAsReadAsync(Guid notificationId);
        Task MarkAllASReadAsync(Guid userId);
        Task DeleteAllReadAsync(Guid userId);
        Task DeleteNotifications(Guid notificationId, Guid userId);

    }
}
