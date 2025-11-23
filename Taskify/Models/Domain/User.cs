using System.ComponentModel.DataAnnotations;

namespace Taskify.Models
{
    public class User
    {
        public int Id { get; set; }                    
        [Required, MaxLength(100)]
        public string FullName { get; set; }             
        [Required, MaxLength(100)]
        public string Email { get; set; }                 
        [Required]
        public string PasswordHash { get; set; }        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;       
    }
}
