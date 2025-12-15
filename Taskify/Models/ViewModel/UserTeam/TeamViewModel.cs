using System.ComponentModel.DataAnnotations;

namespace Taskify.Models
{
    // Dùng cho danh sách Team (Index)
    public class TeamViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string OwnerName { get; set; }
        public string Description { get; set; }
        public bool IsOwner { get; set; } // Để hiện nút Edit/Delete nếu là chủ
        public int MemberCount { get; set; }//
        public int BoardCount { get; set; }

        // Hiển thị vài avatar thành viên đại diện 
        public List<TeamMemberViewModel> Members { get; set; } = new List<TeamMemberViewModel>();
        public List<BoardViewModel> Boards { get; set; }
    }

    // Dùng cho chi tiết Team (Details)
    public class TeamDetailsViewModel : TeamViewModel
    {
        public string OwnerName { get; set; }//
        public DateTime CreatedAt { get; set; }
        // Danh sách đầy đủ thành viên kèm role (nếu có)
        public List<TeamMemberViewModel> AllMembers { get; set; } = new List<TeamMemberViewModel>();

        public List<TeamActivityViewModel> Activities { get; set; }
    }

    public class TeamMemberViewModel : MemberViewModel
    {
        public Guid Id { get; set; }//UserId    
        public string FullName { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public string Role { get; set; } // Owner, Member
        public bool IsOnline { get; set; }
        public bool IsOwner { get; set; }
        public string RoleName { get; set; }
        public DateTime JoinedDate { get; set; }
    }
    public class TeamActivityViewModel
    {
        public string UserAvatar { get; set; }
        public string UserInitials { get; set; }
        public string Content { get; set; }
        public string TimeAgo { get; set; }
    }

    // Dùng cho Form tạo/sửa
    public class TeamCreateViewModel
    {
        [Required(ErrorMessage = "Team name is required")]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }
    }
}