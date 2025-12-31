using Taskify.Models;

namespace Taskify.Services
{
    public interface INotificationService
    {
        Task<List<NotificationViewModel>> GetNotificationsAsync(Guid userId);
        Task CreateInviteNotificationAsync(Guid fromUserId, Guid toUserId, Guid teamId, string teamName);
        Task CreateRemindNotificationAsync(Guid fromUserId, Guid toUserId, Guid teamId, string name, string? message);
        Task CreateInfoNotificationAsync(Guid userId, string message);
        Task<bool> MarkAsReadAsync(Guid notificationId);
        Task MarkAllASReadAsync(Guid userId);
        Task DeleteAllReadAsync(Guid userId);
        Task DeleteNotifications(Guid notificationId, Guid userId);

    }
}
