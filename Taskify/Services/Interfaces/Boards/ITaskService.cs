using Taskify.Models;

namespace Taskify.Services
{
    public interface ITaskService
    {
        Task MoveTaskAsync(Guid taskId, Guid targetListId, int newPosition);

        Task<TaskItem> CreateTaskAsync(TaskCreateViewModel model, Guid userId);
        Task DeleteTaskAsync(Guid taskId);
        Task<TaskItem> GetTaskByIdAsync(Guid taskId);
    }
}
