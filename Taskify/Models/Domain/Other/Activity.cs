using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskify.Models
{
    public class ActivityLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? TeamId { get; set; }//Log Ca nhan thi null
        public Guid UserId { get; set; }
        public Guid? BoardId { get; set; }
        public ActivityType Type { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }=DateTime.Now;

        [ForeignKey("TeamId")]
        public virtual Team? Team { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        [ForeignKey("BoardId")]
        public virtual Board? Board { get; set; }
    }
    public enum ActivityType
    { // Team Scope
        MemberJoined,
        MemberLeft,
        MemberRemoved,
        RoleUpdated,
        TeamCreated,
        TeamDeleted,

        // General Scope (Board/Task)
        BoardCreated,
        BoardDeleted, // (Dành cho trường hợp log mà không gắn BoardId - hiếm dùng nếu dùng cascade)
       
        TaskCreated,
        TaskCompleted,
        TaskDeleted,
        TaskMoved,    // Kéo thả task
        TaskUpdated
    }
}
