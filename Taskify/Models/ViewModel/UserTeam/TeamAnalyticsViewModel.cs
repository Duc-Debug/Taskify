namespace Taskify.Models
{
    public class TeamAnalyticsViewModel
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; }
        public string Decription { get; set; }
        public TeamRole CurrentUserRole { get; set; }

        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTask { get; set; }
        public int OverdueTasks { get; set; }
        public List<MemberPerformanceViewModel> MemberStats { get; set; } = new List<MemberPerformanceViewModel>();
        public List<TaskAlertViewModel> AttentionTasks { get; set; } = new List<TaskAlertViewModel>();

    }
    public class MemberPerformanceViewModel
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
        public string JobTitle { get; set; }
        public int AssignedCount { get; set; }
        public int CompletedCount { get; set; }
        public int OverdueCount { get; set; }
       

        public double CompletionRate => AssignedCount == 0 ? 0 : Math.Round((double)CompletedCount / AssignedCount * 100, 1);
        public bool IsIdle => AssignedCount == 0;

    }
    public class TaskAlertViewModel
    {
        public Guid TaskId { get; set; }
        public string TaskName { get; set; }
        public DateTime DueDate { get; set; }
        public TaskStatus Status { get; set; }

        // Danh sách người được giao task này (để gửi remind cho đúng người)
        public List<MemberPerformanceViewModel> Assignees { get; set; } = new List<MemberPerformanceViewModel>();

        // Cờ đánh dấu: True = Quá hạn, False = Sắp đến hạn
        public bool IsOverdue => DueDate < DateTime.UtcNow;
    }
}
