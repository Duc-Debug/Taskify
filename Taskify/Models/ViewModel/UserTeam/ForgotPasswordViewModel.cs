using System.ComponentModel.DataAnnotations;
namespace Taskify.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Please enter Email")]
        [EmailAddress(ErrorMessage = "Email not right")]
        public string Email { get; set; }
    }
    public class ResetPasswordViewModel
    {
        [Required]
        public string Email { get; set; }
        [Required(ErrorMessage ="Please enter OTP")]
        public string OtpCode { get; set; }
        [Required(ErrorMessage ="Please enter new password")]
        [MinLength(6,ErrorMessage ="Password must at least 6 charater")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage ="Password not similar")]
        public string ConfirmNewPassword { get; set; }
    }
}
