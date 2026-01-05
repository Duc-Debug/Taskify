using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskify.Models
{
    public class TaskItem
    {
        public Guid Id { get; set; }
        [Required, MaxLength(200)]
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid CreatorId { get; set; }
        [ForeignKey("CreatorId")]
        public User Creator { get; set; }
        public int Order { get; set; } // Để sắp xếp thứ tự trên Board
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public TaskStatus Status { get; set; } = TaskStatus.Pending;

       // public DateTime? Deadline { get; set; } 
        public DateTime? CompletedAt { get; set; }
        public Guid ListId { get; set; }
        [ForeignKey("ListId")] //
        public TaskList List { get; set; }

        public int? CategoryId { get; set; } 
        public Category Category { get; set; }

        public ICollection<TaskHistory> TaskHistories { get; set; }

        public ICollection<TaskAssignment> Assignments { get; set; }
        public ICollection<TaskComment> Comments { get; set; }
    }

    public class TaskHistory
    {
        public int Id { get; set; } // ID của history để int cũng được

        // [SỬA LỖI] Phải đổi thành Guid để khớp với TaskItem
        public Guid TaskItemId { get; set; }
        [ForeignKey("TaskItemId")]
        public TaskItem TaskItem { get; set; }

        public string Action { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

}
