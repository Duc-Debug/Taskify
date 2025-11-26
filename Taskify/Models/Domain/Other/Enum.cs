namespace Taskify.Models
{
    public enum TaskStatus
    {
        Pending=1,
        InProgress=2,
        Completed=3,
        Cancelled=4
    }
    public enum TaskPriority
    {
        Low=1, Medium=2, High=3
    }
    public enum TeamRole
    {
        Member = 1,
        Admin=2,
        Owner=3
    }

}
