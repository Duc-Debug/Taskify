using System.ComponentModel.DataAnnotations;

namespace Taskify.Models
{
    public class BoardCreateViewModel
    {
        [Required(ErrorMessage = "Board title is required")]
        [MaxLength(100)]
        public string Name { get; set; }
        [Required]
        public string BoardType { get; set; } = "personal"; // "personal" hoặc "team"
        public string? Description { get; set; }
        public Guid? TeamId { get; set; }
        //Choose Temple: Blank, Kanban, Scrum
        public string Template { get; set; } = "Kanban";
    }
    public class BoardEditViewModel
    {
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
