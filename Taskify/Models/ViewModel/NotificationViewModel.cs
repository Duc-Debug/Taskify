namespace Taskify.Models
{
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class TaskHistoryViewModel
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
    }
    public class SettingsViewModel
    {
        public bool EnableNotification { get; set; }
        public string Theme { get; set; }
    }
}
