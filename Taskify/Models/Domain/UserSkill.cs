using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskify.Models
{
    public class UserSkill
    {
        [Key]
        public int Id { get; set; }

        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        [MaxLength(50)]
        public string SkillName { get; set; } // Ví dụ: "C#", "ReactJS", "Marketing"

        [Range(1, 10)]
        public int ProficiencyLevel { get; set; } // Thang điểm 1-10 để AI đánh giá độ phù hợp
    }
}