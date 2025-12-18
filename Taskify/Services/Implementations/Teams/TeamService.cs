using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Services
{
    public class TeamService : ITeamService
    {
        public AppDbContext _context;
        private readonly INotificationService _notificationService;
        public TeamService(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }
        public async Task<List<TeamViewModel>> GetTeamsByUserIdAsync(Guid userId)
        {
            // Lấy tất cả Team mà User này là thành viên
            var teams = await _context.TeamMembers
                .Include(tm => tm.Team)
                .ThenInclude(t => t.Owner)
                .Include(tm => tm.Team.Members)
                .ThenInclude(m => m.User)
                .Include(tm => tm.Team.Boards)
                .Where(tm => tm.UserId == userId)
                .Select(tm => tm.Team)
                .ToListAsync();
           

            return teams.Select(t => new TeamViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                OwnerName = t.Owner.FullName,
                IsOwner = t.OwnerId == userId,
                MemberCount = t.Members.Count,
                BoardCount = t.Boards.Count,
                Members = t.Members.Take(4).Select(m => new TeamMemberViewModel
                {
                    Id = m.UserId,
                    FullName = m.User.FullName,
                    AvatarUrl = m.User.AvatarUrl,
                    Initials = m.User.FullName.Substring(0, 1)
                }).ToList()
            }).ToList();
        }
        public async Task CreateTeamAsync(TeamCreateViewModel model, Guid userId)
        {
            // 1. Khởi tạo Team mới
            var team = new Team
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                Description = model.Description,
                OwnerId = userId,           
                CreatedAt = DateTime.Now
            };

            // Thêm Team vào DbSet
            _context.Teams.Add(team);

            var initialMember = new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = team.Id,
                UserId = userId,
                Role = TeamRole.Owner,     
                JoinedDate = DateTime.Now
            };

          
            _context.TeamMembers.Add(initialMember);

            // 3. Lưu tất cả xuống DB trong 1 transaction
            await _context.SaveChangesAsync();
        }
        public async Task<TeamDetailsViewModel> GetTeamDetailsAsync(Guid teamId, Guid currentUserId)
        {
            var team = await _context.Teams
                .Include(t => t.Boards).ThenInclude(b => b.Lists).ThenInclude(l => l.Tasks)
                .Include(t => t.Members).ThenInclude(tm => tm.User)
                .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) return null;

            var currentUserMember = team.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (currentUserMember == null) return null;

            var isCurrentUserOwner = currentUserMember.Role == TeamRole.Owner;
            var isCurrentUserAdmin = currentUserMember.Role == TeamRole.Admin;
            return new TeamDetailsViewModel
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                CreatedAt = DateTime.UtcNow,
                IsOwner = isCurrentUserOwner,
                IsAdmin = isCurrentUserAdmin,
                MemberCount = team.Members.Count,
                IsInviteApprovalRequired=team.IsInviteApprovalRequired,

                Boards = team.Boards.Select(b => new BoardViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    Lists = b.Lists.Select(l => new TaskListViewModel
                    {
                        Tasks = l.Tasks.Select(t => new TaskCardViewModel()).ToList()
                    }).ToList(),
                }).ToList(),
                Members = team.Members.Select(tm => new TeamMemberViewModel
                {
                    Id = tm.UserId,
                    FullName = tm.User.FullName,
                    Email = tm.User.Email,
                    AvatarUrl = tm.User.AvatarUrl,
                    Initials = tm.User.FullName?.Substring(0, 1) ?? "U",
                    IsOwner = tm.Role == TeamRole.Owner,

                    RoleName = tm.Role.ToString(),
                    JoinedDate = DateTime.UtcNow, // Phai them Field JoinedDate vao bang TeamMember
                    IsOnline = new Random().Next(0, 2) == 1 // Random vui vui
                }).OrderByDescending(m => m.IsOwner).ToList()
            };
        }
        public async Task UpdateTeamAsync(TeamEditViewModel model, Guid userId)
        {
            var team = await _context.Teams.FindAsync(model.Id);
            if (team == null) throw new Exception("Not exists Team");
            if (team.OwnerId != userId) throw new Exception("You don't permission edit this Team");
            team.Name= model.Name;
            team.Description = model.Description;
            await _context.SaveChangesAsync();
        }
        public async Task DeleteTeamAsync(Guid teamId,Guid userId)
        {
            var team = await _context.Teams
                    .Include(m => m.Members)
                    .Include(m => m.Boards)
                    .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) throw new Exception("Team no Found");
            if (team.OwnerId != userId) throw new Exception("You are not perrmission delete");
            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
        }
        public async Task<bool> RemoveMemberAsync(Guid teamId, Guid memberId, Guid currentUserId)
        {
            var currentUserRole = await _context.TeamMembers
                 .Where(tm => tm.TeamId == teamId && tm.UserId == currentUserId)
                 .Select(tm => tm.Role)
                 .FirstOrDefaultAsync();
            if (currentUserRole != TeamRole.Owner) return false;
            var memberToRemove = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == memberId);
            if (memberToRemove == null) return false;
            if (memberToRemove.UserId == currentUserId) return false;
            _context.TeamMembers.Remove(memberToRemove);
            await _context.SaveChangesAsync();
            return true;
        }
        //OTHER
        public async Task<(bool Success, string Message)> InviteMemberAsync(Guid teamId, string email, Guid senderId)
        {
            var userToInvite = _context.Users.FirstOrDefault(u => u.Email == email);
            if (userToInvite == null) return (false, "Email user doesn't exsist.");

            // Check xem đã là thành viên chưa
            var isAlreadyMember = await _context.TeamMembers
                .AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userToInvite.Id);
            if (isAlreadyMember) return (false, "User is a member Team.");

            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return (false, "Team not found.");

            // Lấy Role người gửi lời mời
            var senderRole = await GetUserRoleInTeamAsync(teamId, senderId); // Hàm check role viết ở dưới

            // --- LOGIC PHÂN QUYỀN MỜI ---

            // 1. Nếu là Admin mời VÀ Team yêu cầu duyệt
            if (senderRole == TeamRole.Admin && team.IsInviteApprovalRequired)
            {
                // Tạo thông báo gửi cho Owner
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = team.OwnerId, // Người nhận là Owner
                    SenderId = senderId,   // Người gửi là Admin
                    Type = NotificationType.ApprovalRequest, // Loại: Yêu cầu duyệt
                    Message = $"Admin want to invite {userToInvite.FullName} ({userToInvite.Email}) join team.",
                    ReferenceId = teamId,
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    // QUAN TRỌNG: Lưu ID người được mời vào đây để dùng sau này
                    Metadata = userToInvite.Id.ToString()
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return (true, "Request send to Owner in approve.");
            }

            // 2. Nếu là Owner HOẶC (Admin và Team KHÔNG cần duyệt) -> Gửi lời mời trực tiếp
            if (senderRole == TeamRole.Owner || (senderRole == TeamRole.Admin && !team.IsInviteApprovalRequired))
            {
                await _notificationService.CreateInviteNotificationAsync(senderId, userToInvite.Id, teamId, team.Name);
                return (true, "The invitation send successfully");
            }

            return (false, "You are not permisson to invite member");
        }
      
        public async Task<bool> HandleInviteApprovalAsync(Guid notificationId, bool isApproved)
        {
            // 1. Lấy thông báo yêu cầu duyệt
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            // Kiểm tra null và đúng loại thông báo
            if (notification == null || notification.Type != NotificationType.ApprovalRequest)
                return false;

            // 2. Nếu Owner TỪ CHỐI (Reject)
            if (!isApproved)
            {
                // Gửi thông báo lại cho Admin (SenderId) biết là bị từ chối
                if (notification.SenderId.HasValue)
                {
                    await _notificationService.CreateInfoNotificationAsync(
                        notification.SenderId.Value, // <--- SỬA: Gửi cho Admin
                        "Your member invitation request has been denied.." 
                    );
                }

                // Xóa đơn xin phép
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                return true;
            }

            // 3. Nếu Owner ĐỒNG Ý (Approve)
            if (!string.IsNullOrEmpty(notification.Metadata) && Guid.TryParse(notification.Metadata, out Guid userToInviteId))
            {
                if (!notification.ReferenceId.HasValue) return false;

                var teamId = notification.ReferenceId.Value;
                var team = await _context.Teams.FindAsync(teamId);

                if (team != null)
                {
                    // A. Gửi lời mời chính thức cho người được mời (Invite Notification)
                    // Người gửi (Sender) bây giờ là Owner (notification.UserId)
                    await _notificationService.CreateInviteNotificationAsync(
                        notification.UserId, // Owner gửi
                        userToInviteId,      // Người nhận (khách)
                        teamId,
                        team.Name
                    );

                    // B. Báo tin lại cho Admin (người gửi yêu cầu duyệt lúc đầu)
                    if (notification.SenderId.HasValue)
                    {
                        await _notificationService.CreateInfoNotificationAsync(
                            notification.SenderId.Value, // <--- SỬA: Gửi cho Admin
                            "The owner has approved your member invitation request.."
                        );
                    }
                }

                // C. Xóa đơn xin phép
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        public async Task<(bool Success, string Message)> RespondInvitationAsync(Guid notificationId, Guid userId, bool isAccepted)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null || notification.UserId != userId)
            {
                return (false, "Notification not found or access denied.");
            }
            var teamId = notification.ReferenceId.Value;
            var senderId = notification.SenderId.Value;
            if (isAccepted)
            {
                var exists = await _context.TeamMembers
                    .AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
                if (!exists)
                {
                    var newMember = new TeamMember
                    {
                        Id = Guid.NewGuid(),
                        TeamId = teamId,
                        UserId = userId,
                        Role = TeamRole.Member,
                        JoinedDate = DateTime.Now
                    };
                    _context.TeamMembers.Add(newMember);

                    var userAccepting = await _context.Users.FindAsync(userId);
                    await _notificationService.CreateInfoNotificationAsync(senderId, $"User {userAccepting.FullName} has accepted the invitation to join the team.");
                }
            }
            else
            {
                var userDeclining = await _context.Users.FindAsync(userId);
                await _notificationService.CreateInfoNotificationAsync(senderId, $"User {userDeclining.FullName} has declined the invitation to join the team.");

            }
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return (true, isAccepted ? "Invitation accepted." : "Invitation declined.");
        }
        //=========SETTING============
       public async Task UpdateSettingsTeam(TeamSettingViewModel model,Guid userId)
        {
            var team = await _context.Teams.FindAsync(model.TeamId);
            if (team == null) throw new Exception();
            if(team.OwnerId != userId) throw new Exception("Only the Owner has the right to change this setting..");
            team.IsInviteApprovalRequired = model.IsInviteApprovalRequired;
            await _context.SaveChangesAsync();
        }
        //==================HELPER===============
        public async Task<(bool Success, string Message)> ChangeMemberRoleAsync(Guid teamId, Guid memberId, TeamRole newRole, Guid currentUserId)
        {
            var currentUserMember = await _context.TeamMembers
                 .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == currentUserId);
            if (currentUserMember == null || currentUserMember.Role != TeamRole.Owner)
            {
                return (false, "Only team owners can change member roles.");
            }
            var targetMember = await _context.TeamMembers
                .Include(tm => tm.User)
                .Include(tm => tm.Team)
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == memberId);
            if (targetMember == null) return (false, "Member not found in the team.");
            if (targetMember.UserId == currentUserId)
            {
                return (false, "Owners cannot change their own role.");
            }
            if (newRole == TeamRole.Owner)
            {
                currentUserMember.Role = TeamRole.Admin;
                targetMember.Role = TeamRole.Owner;

                var team = await _context.Teams.FindAsync(teamId);
                if (team != null) team.OwnerId = targetMember.UserId;

                await _context.SaveChangesAsync();
                await _notificationService.CreateInfoNotificationAsync(memberId, $"You have been promoted to Owner of the team '{targetMember.Team.Name}'.");
                await _notificationService.CreateInfoNotificationAsync(currentUserId, $"You have transferred ownership of the team '{targetMember.Team.Name}' to {targetMember.User.FullName}.");
                return (true, "Member promoted to Owner successfully.");
            }
            targetMember.Role = newRole;
            await _context.SaveChangesAsync();
            await _notificationService.CreateInfoNotificationAsync(memberId, $"Your role in team '{targetMember.Team.Name}' has been changed to {newRole}.");
            return (true, "Member role updated successfully.");
        }
        public async Task<TeamRole> GetUserRoleInTeamAsync(Guid? teamId, Guid userId)
        {
            var member = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
            return member.Role;
        }
        public async Task<TeamRole?> GetUserRoleInTeamAsync(Guid teamId, Guid userId)
        {
            var member = await _context.TeamMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
            return member?.Role;
        }
        public async Task<TeamEditViewModel> GetTeamForEditAsync(Guid teamId)
        {
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return null;
            return new TeamEditViewModel
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description
            };
        }
        public async Task<TeamSettingViewModel> GetTeamSettingsASync(Guid teamId)
        {
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) throw new Exception(" ");
            return new TeamSettingViewModel
            {
                TeamId = team.Id,
                TeamName = team.Name,
                IsInviteApprovalRequired = team.IsInviteApprovalRequired
            };

        }
    }
}
