using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Taskify.Models
{
    // 1. Dùng để hiển thị cái thẻ nhỏ xíu trên cột (Kanban Card)
    public class TaskCardViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public int CommentsCount { get; set; }
        public int AttachmentsCount { get; set; }

        public TaskStatus Status { get; set; }

        // Hiển thị avatar những người được giao
        public List<MemberViewModel> Assignees { get; set; } = new List<MemberViewModel>();
    }

    // 2. Dùng để hiển thị chi tiết khi bấm vào Modal (Popup)
    public class TaskDetailsViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public TaskStatus Status { get; set; }
        public Guid ListId { get; set; }
        public string ListName { get; set; }

        public string CreatorName { get; set; }

        // Danh sách người được giao
        public List<MemberViewModel> Assignees { get; set; } = new List<MemberViewModel>();

        // Lịch sử hoạt động
        public List<TaskHistoryViewModel> Activities { get; set; } = new List<TaskHistoryViewModel>();
    }

    // 3. Dùng cho Form tạo mới Task
    public class TaskCreateViewModel
    {
        [Required(ErrorMessage = "Title cannot be blank")]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public Guid ListId { get; set; } // Bắt buộc phải biết nằm ở cột nào

        public Guid BoardId { get; set; } // Để redirect về đúng board sau khi tạo

        public TaskPriority Priority { get; set; }

        public DateTime? DueDate { get; set; }

        // Danh sách ID của user được assign (từ dropdown)
        public List<Guid> SelectedAssigneeIds { get; set; } = new List<Guid>();
    }

    // Helper class cho Avatar thành viên
    public class MemberViewModel
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; } // Nếu có
        public string Initials { get; set; } // Chữ cái đầu tên (VD: "NV")
    }
    public class TaskEditViewModel
    {
        [Required]
        public Guid Id { get; set; } // Quan trọng nhất: Sửa task nào?

        [Required(ErrorMessage = "Title cannot be blank")]
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime? DueDate { get; set; }

        // Để biết sau khi sửa xong thì redirect về Board nào
        public Guid BoardId { get; set; }

        // Có thể cho phép đổi cột (List) ngay trong form sửa
        public Guid ListId { get; set; }

        // Danh sách ID thành viên đã được assign (để hiển thị selected trong dropdown)
        public List<Guid> SelectedAssigneeIds { get; set; } = new List<Guid>();

        // Danh sách chọn (Dropdown data) - Optional, dùng để pass dữ liệu sang View
        public List<SelectListItem> AvailableMembers { get; set; } = new List<SelectListItem>();
    }
    public class TaskHistoryViewModel
    {
        public string Action { get; set; }
        public string UserFullName { get; set; }
        public DateTime Timestamp { get; set; }
    }

}
