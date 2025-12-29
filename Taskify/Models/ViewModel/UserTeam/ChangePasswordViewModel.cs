using System.ComponentModel.DataAnnotations;

namespace Taskify.Models
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Please enter old Password")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }
        [Required(ErrorMessage ="Please enter new password")]
        [MinLength(6, ErrorMessage ="Password at least 6 charaters")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Password not compare")]
        public string ConfirmNewPassword { get; set; }
    }
}
