using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskify.Models
{
    public class Team
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Key cho người tạo (Owner)
        public Guid OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        public User Owner { get; set; }

        // Navigation
        public ICollection<TeamMember> Members { get; set; }
        public ICollection<Board> Boards { get; set; }
    }
}