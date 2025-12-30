using System.ComponentModel.DataAnnotations;

namespace Taskify.Models
{
    public class ProfileViewModel
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; }
        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? Bio { get; set; }
        // Job, Skill
        public string? JobTitle { get; set; }
        public string? SeniorityLevel { get; set; }
        public List<UserSkillViewModel> Skills { get; set; } = new List<UserSkillViewModel>();

        public string? AvatarUrl { get; set; }
        public DateTime MemberSince { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public bool EmailNotifications { get; set; }
        public bool TaskReminders { get; set; }
    }
    public class UserSkillViewModel{
        public string SkillName { get; set; }
        public int ProficiencyLevel { get; set; }
    }
}
