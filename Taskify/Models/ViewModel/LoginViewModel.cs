using System.ComponentModel.DataAnnotations;

namespace Taskify.Models
{
    public class LoginViewModel
    {
        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; }
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }
    public class RegisterViewModel
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; }
        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; }
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
        [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
        [Required, DataType(DataType.Password)]
        public bool AcceptTerms { get; set; }
    }
}
