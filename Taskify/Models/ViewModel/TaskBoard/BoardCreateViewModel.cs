using System.ComponentModel.DataAnnotations;

namespace Taskify.Models
{
    public class BoardCreateViewModel
    {
        [Required(ErrorMessage = "Board title is required")]
        [MaxLength(100)]
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid? TeamId { get; set; }
        //Choose Temple: Blank, Kanban, Scrum
        public string Template { get; set; } = "Kanban";
    }
    public class BoardEditViewModel
    {

    }
}
