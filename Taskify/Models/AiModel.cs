using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Taskify.Models
{
    // Cấu trúc dữ liệu chúng ta mong đợi AI trả về
    public class AiBoardPlan
    {
        public string BoardName { get; set; }
        public string Description { get; set; }
        public List<AiListPlan> Lists { get; set; }
    }

    public class AiListPlan
    {
        public string Title { get; set; }
        public List<AiTaskPlan> Tasks { get; set; }
    }

    public class AiTaskPlan
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; } // Low, Medium, High

        // AI sẽ trả về UserID của nhân viên mà nó chọn
        public Guid? AssignedUserId { get; set; }
        public string ReasonForAssignment { get; set; } // Lý do chọn (để debug hoặc hiển thị)
    }
    public class CreateBoardAiViewModel
    {
        [Required(ErrorMessage = "Please enter decription project")]
        [Display(Name = "Project description")]
        public string Prompt { get; set; }

        // True: Team Board, False: Personal Board
        public bool IsTeamBoard { get; set; } = false;

        [Display(Name = "Choose Team")]
        public Guid? TeamId { get; set; }

        public SelectList? Teams { get; set; }
    }
}
