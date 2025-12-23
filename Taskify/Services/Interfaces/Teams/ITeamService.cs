using Taskify.Models;

namespace Taskify.Services
{
    public interface ITeamService
    {
        //CRUD
        Task<List<TeamViewModel>> GetTeamsByUserIdAsync(Guid userId);
        Task CreateTeamAsync(TeamCreateViewModel model, Guid userId);
        Task<TeamDetailsViewModel> GetTeamDetailsAsync(Guid teamId, Guid currentUserId);
        Task UpdateTeamAsync(TeamEditViewModel model, Guid userId);
        Task DeleteTeamAsync(Guid teamId, Guid userId);
        Task<bool> RemoveMemberAsync(Guid teamId, Guid memberId, Guid currentUserId);
        Task LeaveTeamAsync(Guid teamId, Guid userId);
        //OTHER
        Task<(bool Success, string Message)> InviteMemberAsync(Guid teamId, string email, Guid senderId);
        Task<(bool Success, string Message)> RespondInvitationAsync(Guid notificationId, Guid userId, bool isAccepted);
        Task<(bool Success, string Message)> ChangeMemberRoleAsync(Guid teamId,  Guid memberId,TeamRole newRole,Guid currentUserId);
        Task UpdateSettingsTeam(TeamSettingViewModel model, Guid userId);
        //HELPER
        Task<TeamRole> GetUserRoleInTeamAsync(Guid? teamId, Guid userId);
        Task<bool> HandleInviteApprovalAsync(Guid notificationId, bool isApproved);
        Task<TeamRole?> GetUserRoleInTeamAsync(Guid teamId, Guid userId);
        Task<TeamEditViewModel> GetTeamForEditAsync(Guid teamId);
        Task<TeamSettingViewModel> GetTeamSettingsASync(Guid teamId);
    }
}
