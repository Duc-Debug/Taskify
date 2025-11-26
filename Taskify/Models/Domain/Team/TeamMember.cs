using System.ComponentModel.DataAnnotations.Schema;

namespace Taskify.Models
{
    public class TeamMember
    {
        public Guid Id { get; set; }

        public Guid TeamId { get; set; }
        [ForeignKey("TeamId")]
        public Team Team { get; set; }

        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
      
        public TeamRole Role { get; set; } = TeamRole.Member;
    }
}
