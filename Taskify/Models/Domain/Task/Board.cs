using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Taskify.Models
{
    public class Board
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        // [QUAN TRỌNG] Thêm người sở hữu (Owner)
        // Đây là người tạo ra bảng này (dù là bảng cá nhân hay team)
        public Guid OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public User Owner { get; set; }

        // [SỬA] Cho phép null (Guid?)
        // Null = Bảng cá nhân. Có giá trị = Bảng của Team.
        public Guid? TeamId { get; set; }
        [ForeignKey("TeamId")]
        public Team? Team { get; set; }

        public ICollection<TaskList> Lists { get; set; }
    }
}
