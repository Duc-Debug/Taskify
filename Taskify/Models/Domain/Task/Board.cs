using System.Diagnostics;

namespace Taskify.Models
{
    public class Board
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public Guid TeamId { get; set; }

        // Navigation
        public Team Team { get; set; }
        public ICollection<TaskList> Lists { get; set; }
    }
}
