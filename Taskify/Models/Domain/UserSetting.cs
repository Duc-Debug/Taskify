namespace Taskify.Models
{
    public class UserSetting
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public bool EnableNotifications { get; set; } = true;
        public string Theme { get; set; } = "Light";
    }
}
