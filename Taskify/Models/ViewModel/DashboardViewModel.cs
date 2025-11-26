namespace Taskify.Models
{
    public class DashboardViewModel
    {
        // Thống kê cá nhân
        public int TotalAssignedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int CompletedTasks { get; set; }

        // Danh sách các nhóm của tôi
        public List<TeamSummaryViewModel> MyTeams { get; set; } = new List<TeamSummaryViewModel>();
    }

    public class TeamSummaryViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MemberCount { get; set; }

        // Danh sách các bảng (Boards) trong team này
        public List<BoardSummaryViewModel> Boards { get; set; } = new List<BoardSummaryViewModel>();
    }

    public class BoardSummaryViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
