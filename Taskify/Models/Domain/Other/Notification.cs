namespace Taskify.Models
{
    public enum NotificationType
    {
        Info =0,    //Thuong
        TeamInvite=1    // Loi moi Team
    }
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        //
        public NotificationType Type { get; set; } = NotificationType.Info;
        public Guid? SenderId { get; set; }
        public Guid? ReferenceId { get; set; } // Team Id luu day
    }
}
