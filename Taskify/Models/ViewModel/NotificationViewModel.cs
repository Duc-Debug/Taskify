namespace Taskify.Models
{
    public class NotificationViewModel
    {
        public Guid Id { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public NotificationType Type { get; set; } // Dòng này quan trọng
        public Guid? SenderId { get; set; }
        public Guid? ReferenceId { get; set; }
    }   
    public class SettingsViewModel
    {
        public bool EnableNotification { get; set; }
        public string Theme { get; set; }
    }
}
