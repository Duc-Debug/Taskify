using Taskify.Models;

namespace Taskify.Services
{
    public interface ITaskService
    {
        Task MoveTaskAsync(Guid taskId, Guid targetListId, int newPosition);

        Task<TaskItem> CreateTaskAsync(TaskCreateViewModel model, Guid userId);
        Task DeleteTaskAsync(Guid taskId);
        Task<TaskItem> GetTaskByIdAsync(Guid taskId);
        Task<TaskDetailsViewModel> GetTaskDetailsAsync(Guid taskId);
        Task<List<TaskDetailsViewModel>> GetTasksByUserIdAsync(Guid userId);
        Task UpdateTaskAsync(TaskEditViewModel model, Guid userId);
        Task<TaskEditViewModel> GetTaskForEditAsync(Guid taskId); // Ham lay du lieu de edit
    }
}
