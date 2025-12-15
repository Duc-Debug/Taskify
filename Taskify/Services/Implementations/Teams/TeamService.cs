using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Services
{
    public class TeamService : ITeamService
    {
        public AppDbContext _context;
        private readonly INotificationService _notificationService;
        public TeamService(AppDbContext context,INotificationService notificationService)
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
                OwnerId = userId,           // Gán khóa ngoại Owner
                CreatedAt = DateTime.Now
            };

            // Thêm Team vào DbSet
            _context.Teams.Add(team);

            // 2. Tự động thêm người tạo thành "Thành viên đầu tiên" (Role = Owner)
            var initialMember = new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = team.Id,
                UserId = userId,
                Role = TeamRole.Owner,      // Quan trọng: Phải là Owner
                JoinedDate = DateTime.Now
            };

            // Thêm Member vào DbSet
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

            return new TeamDetailsViewModel
            {
                Id = team.Id,
                Name = team.Name,
                Description = "A collaborative space for our projects.",//DB chua co, de explamle th
                CreatedAt = DateTime.UtcNow,
                IsOwner = isCurrentUserOwner,
                MemberCount = team.Members.Count,

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
            if(memberToRemove.UserId== currentUserId) return false;
            _context.TeamMembers.Remove(memberToRemove);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> InviteMemberAsync(Guid teamId, string email, Guid senderId)
        {
            var userToInvite = _context.Users.FirstOrDefault(u => u.Email == email);
            if (userToInvite == null)
            {
                return (false, "User with this email does not exist.");
            }

           var isAlreadyMember = await _context.TeamMembers
                .AnyAsync(tm=>tm.TeamId== teamId && tm.UserId== userToInvite.Id);
            if (isAlreadyMember) return (false, " User is already a member of the team.");

            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return (false, "Team not found.");
            await _notificationService.CreateInviteNotificationAsync(senderId,userToInvite.Id,teamId,team.Name);
            return (true, "Invitation sent successfully.");
        }

        public async Task<(bool Success, string Message)> RespondInvitationAsync(Guid notificationId, Guid userId, bool isAccepted)
        {
           var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null || notification.UserId != userId)
            {
                return (false, "Notification not found or access denied.");
            }
            var teamId = notification.ReferenceId.Value;
            var senderId= notification.SenderId.Value;
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

        public async Task<(bool Success, string Message)> ChangeMemberRoleAsync(Guid teamId, Guid memberId, TeamRole newRole, Guid currentUserId)
        {
           var currentUserMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm=>tm.TeamId ==teamId &&tm.UserId==currentUserId);
            if(currentUserMember == null || currentUserMember.Role != TeamRole.Owner)
            {
                return (false, "Only team owners can change member roles.");
            }
            var targetMember = await _context.TeamMembers
                .Include(tm=>tm.User)
                .Include(tm=>tm.Team)
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == memberId);
            if (targetMember == null) return (false, "Member not found in the team.");
            if(targetMember.UserId == currentUserId)
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
    }
}
