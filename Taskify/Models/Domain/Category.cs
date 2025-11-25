using System.ComponentModel.DataAnnotations;

namespace Taskify.Models
{
    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        // Navigation
        public ICollection<TaskItem> Tasks { get; set; }
    }
}
