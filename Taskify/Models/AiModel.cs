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
        public Guid? AssignedUserId { get; set; }
        public string ReasonForAssignment { get; set; }
        public int DueInDays { get; set; }
        public int SuccessConfidence { get; set; }
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

    //PREVIEW 
    public class AiPreviewViewModel
    {
        // Dữ liệu gốc để gửi lại Server khi bấm Save (Serialize thành JSON String)
        public string RawPlanJson { get; set; }
        public Guid? TeamId { get; set; }
        public bool IsTeamBoard { get; set; }

        // Dữ liệu hiển thị
        public string BoardName { get; set; }
        public string Description { get; set; }
        public List<AiListPreview> Lists { get; set; } = new List<AiListPreview>();
    }

    public class AiListPreview
    {
        public string Title { get; set; }
        public List<AiTaskPreview> Tasks { get; set; }
    }

    public class AiTaskPreview
    {
        public string Title { get; set; }
        public string Priority { get; set; }
        public int DueInDays { get; set; }

        // Thông tin người được gán + Tỷ lệ thành công
        public Guid? AssignedUserId { get; set; }
        public string AssignedUserName { get; set; }
        public string AssignedUserAvatar { get; set; } // Nếu có
        public int SuccessProbability { get; set; } // Điểm số tính toán
        public string AiReason { get; set; }
    }
}
