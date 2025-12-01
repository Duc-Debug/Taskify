using Taskify.Models;

namespace Taskify.Services
{
    public interface ITeamService
    {
        Task<List<TeamViewModel>> GetTeamsByUserIdAsync(Guid userId);
        Task CreateTeamAsync(TeamCreateViewModel model, Guid userId);
        Task<TeamDetailsViewModel> GetTeamDetailsAsync(Guid teamId, Guid currentUserId);
        Task<bool> RemoveMemberAsync(Guid teamId, Guid memberId, Guid currentUserId);
    }
}
