using Taskify.Models;

namespace Taskify.Services
{
    public interface ITeamService
    {
        Task<List<TeamViewModel>> GetTeamsByUserIdAsync(Guid userId);
        Task CreateTeamAsync(TeamCreateViewModel model, Guid userId);
        Task<TeamDetailsViewModel> GetTeamDetailsAsync(Guid teamId, Guid currentUserId);
        Task<bool> RemoveMemberAsync(Guid teamId, Guid memberId, Guid currentUserId);
        //Inviute
        Task<(bool Success, string Message)> InviteMemberAsync(Guid teamId, string email, Guid senderId);
        Task<(bool Success, string Message)> RespondInvitationAsync(Guid notificationId, Guid userId, bool isAccepted);
        Task<(bool Success, string Message)> ChangeMemberRoleAsync(Guid teamId,  Guid memberId,TeamRole newRole,Guid currentUserId);
    }
}
