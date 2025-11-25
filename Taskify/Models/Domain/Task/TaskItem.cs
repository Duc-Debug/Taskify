using System.ComponentModel.DataAnnotations;

namespace Taskify.Models
{
    public class TaskItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? DueDate { get; set; }
        public int Order { get; set; }

        public Guid ListId { get; set; }
        public Guid? CategoryId { get; set; }

        // Navigation
        public TaskList List { get; set; }
        public Category Category { get; set; }
        public ICollection<TaskAssignment> Assignments { get; set; }
        public ICollection<TaskComment> Comments { get; set; }
    }
    public class TaskHistory
    {
        public int Id { get; set; }
        public int TaskItemId { get; set; }
        public TaskItem TaskItem { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

}
