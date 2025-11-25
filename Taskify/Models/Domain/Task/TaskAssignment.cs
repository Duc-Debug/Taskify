namespace Taskify.Models
{
    public class TaskAssignment
    {
        public Guid Id { get; set; }

        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }

        // Navigation
        public TaskItem Task { get; set; }
        public User User { get; set; }
    }

}
