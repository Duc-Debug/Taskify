using Taskify.Models;

namespace Taskify.Services
{
    public interface IActivityLogService
    {
        Task LogAsync(Guid actorId, ActivityType type, string content, Guid? teamId = null, Guid? boardId = null);
        Task<List<ActivityLog>> GetTeamActivitesAsync(Guid teamId, int count = 20);
        Task<List<ActivityLog>> GetBoardActivitiesAsync(Guid boardId, int count = 50);
        Task CleanupOldLogsASync();

    }
}
