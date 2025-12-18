namespace Taskify.Models
{
    public class BoardViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid TeamId { get; set; }
        public string TeamName { get; set; }
        public Guid OwnerId { get; set; }
        public string? Desciption { get; set; }

        // Danh sách các cột (Lists) trong Board
        public List<TaskListViewModel> Lists { get; set; } = new List<TaskListViewModel>();

        // Danh sách thành viên của Team (để hiển thị filter hoặc assign)
        public List<MemberViewModel> TeamMembers { get; set; } = new List<MemberViewModel>();
        public bool CanCreateList { get; set; }
        public List<ActivityLog> Activities { get; set; }
    }

    public class TaskListViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public int Order { get; set; }

        // Các Task nằm trong cột này
        public List<TaskCardViewModel> Tasks { get; set; } = new List<TaskCardViewModel>();
    }
}
