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
        public string Address { get; set; }
        public string Bio { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime MemberSince { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public bool EmailNotifications { get; set; }
        public bool TaskReminders { get; set; }
    }
    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        public string CurrentPassword { get; set; }
        [Required, DataType(DataType.Password), MinLength(6)]
        public string NewPassword { get; set; }
        [Required, DataType(DataType.Password), Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; }
    }
}
