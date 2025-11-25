namespace Taskify.Models
{
    public class TaskList
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public int Order { get; set; }

        public Guid BoardId { get; set; }

        // Navigation
        public Board Board { get; set; }
        public ICollection<TaskItem> Tasks { get; set; }
    }
}
