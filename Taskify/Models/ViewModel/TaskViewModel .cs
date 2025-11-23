using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Taskify.Models
{
    public class TaskViewModel
    {
        public string Status { get; set; }
        public bool IsCompleted { get; set; }
        public int Id { get; set; }
        public int Priority { get; set; }
    }
    public class TaskCreateViewModel
    {
        [Required, MaxLength(200)]
        public string Title { get; set; }
        [MaxLength(500)]
        public string Description { get; set; }
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }
        public DateTime? DueTime { set; get; }
        public int EstimatedHours { get; set; }
        public int SetReminder { get; set; }
        public string Tags { get; set; }
        public string Status { get; set; }
        public TaskPriority Priority { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public IEnumerable<SelectListItem> Categories { get; set; }
    }
    public class TaskEditViewModel : TaskCreateViewModel
    {
        [Required]
        public int Id { get; set; }
        public TaskStatus Status { get; set; }
    }
    public class TaskDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Decription { get; set; }
        public DateTime? DueDate { get; set; }
        public TaskPriority Priority { get; set; }
        public TaskStatus Status { get; set; }
        public string CategoryName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

}
