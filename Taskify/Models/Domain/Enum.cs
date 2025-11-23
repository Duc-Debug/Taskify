namespace Taskify.Models
{
    public enum TaskStatus
    {
        Pending,    // Chưa làm
        InProgress, // Đang làm
        Completed,  // Hoàn thành
        Cancelled   // Hủy bỏ
    }

    public enum TaskPriority
    {
        Low,
        Medium,
        High
    }

}
