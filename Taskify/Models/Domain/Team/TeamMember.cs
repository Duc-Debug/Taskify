namespace Taskify.Models
{
    public class TeamMember
    {
        public Guid Id { get; set; }

        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; }   // Owner, Admin, Member

        // Navigation
        public Team Team { get; set; }
        public User User { get; set; }
    }
}
