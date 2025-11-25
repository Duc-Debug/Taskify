using System.ComponentModel.DataAnnotations;

namespace Taskify.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public string PasswordHash { get; set; }

        // Navigation
        public ICollection<TeamMember> TeamMembers { get; set; }
        public ICollection<TaskAssignment> TaskAssignments { get; set; }
        public ICollection<TaskComment> Comments { get; set; }
    }
}
