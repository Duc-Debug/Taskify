namespace Taskify.Models
{
    public class TaskComment
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }

        // Navigation
        public TaskItem Task { get; set; }
        public User User { get; set; }
    }

}
