using Taskify.Models;

namespace Taskify.Services
{
    public interface ITaskService
    {
        //Move
        Task MoveTaskAsync(Guid taskId, Guid targetListId, int newPosition,Guid userId);
        //CRUD Task
        Task<TaskItem> CreateTaskAsync(TaskCreateViewModel model, Guid userId);
        Task<(bool Success,string Message)> DeleteTaskAsync(Guid taskId,Guid userId);
        Task<(bool Success, string Message)> UpdateTaskAsync(TaskEditViewModel model, Guid userId, TeamRole? userRole);
        //Get 
        Task<TaskEditViewModel> GetTaskForEditAsync(Guid taskId); // Ham lay du lieu de edit
        Task<TaskItem> GetTaskByIdAsync(Guid taskId);
        Task<TaskDetailsViewModel> GetTaskDetailsAsync(Guid taskId);
        Task<List<TaskDetailsViewModel>> GetTasksByUserIdAsync(Guid userId);
        Task<TeamRole?> GetUserRoleInBoardAsync(Guid boardId, Guid userId);
        Task<Guid?> GetTeamIdByAsync(Guid boardId);
        //Assign
         Task AssignMemberASync(Guid taskId, Guid userId);
        Task RemoveMemberAsync(Guid taskId, Guid userId);
    }
}
