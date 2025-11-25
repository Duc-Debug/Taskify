namespace Taskify.Models
{
    public class Team
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        // Navigation
        public ICollection<TeamMember> Members { get; set; }
        public ICollection<Board> Boards { get; set; }
    }
}
