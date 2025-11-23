using System.ComponentModel.DataAnnotations;

namespace Taskify.Models
{
    public class Category
    {
        public int Id { get; set; }                         // PK
        [Required, MaxLength(50)]
        public string Name { get; set; }                    // Tên danh mục
        public string Description { get; set; }            // Mô tả
        public int UserId { get; set; }                     // FK User
        public User User { get; set; }
    }
}
