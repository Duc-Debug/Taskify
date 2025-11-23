namespace Taskify.Models
{
    public class DashboardViewModel
    {
        public int TotalTasks { get; set; }
        public int PendingTask { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public IEnumerable<TaskDetailsViewModel> UpcomingTasks { get; set; }
    }
}
