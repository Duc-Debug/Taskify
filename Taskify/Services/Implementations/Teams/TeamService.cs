using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Services
{
    public class TeamService : ITeamService
    {
        public AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;
        public TeamService(AppDbContext context, INotificationService notificationService, IActivityLogService activityLogService)
        {
            _context = context;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }
        public async Task<List<TeamViewModel>> GetTeamsByUserIdAsync(Guid userId)
        {
            // Lấy tất cả Team mà User là thành viên
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
            var team = new Team
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                Description = model.Description,
                OwnerId = userId,
                CreatedAt = DateTime.Now
            };

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
            await _activityLogService.LogAsync(userId, ActivityType.TeamCreated, $"Created Team", teamId: team.Id, boardId: null);
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

            var activities = await _activityLogService.GetTeamActivitesAsync(teamId);
            return new TeamDetailsViewModel
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                CreatedAt = DateTime.UtcNow,
                IsOwner = isCurrentUserOwner,
                IsAdmin = isCurrentUserAdmin,
                MemberCount = team.Members.Count,
                Activities = activities ?? new List<ActivityLog>(),
                IsInviteApprovalRequired = team.IsInviteApprovalRequired,

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
                    JoinedDate = DateTime.UtcNow, // Phai them Field JoinedDate vao bang TeamMember, ko co nen de tamm
                    IsOnline = new Random().Next(0, 2) == 1 // Random vui vui, chả có tác dụng 
                }).OrderByDescending(m => m.IsOwner).ToList()
            };
        }
        public async Task UpdateTeamAsync(TeamEditViewModel model, Guid userId)
        {
            var team = await _context.Teams.FindAsync(model.Id);
            if (team == null) throw new Exception("Not exists Team");
            if (team.OwnerId != userId) throw new Exception("You don't permission edit this Team");
            team.Name = model.Name;
            team.Description = model.Description;
            await _context.SaveChangesAsync();
        }
        public async Task DeleteTeamAsync(Guid teamId, Guid userId)
        {
            var team = await _context.Teams
                    .Include(m => m.Members)
                    .Include(m => m.Boards)
                    .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) throw new Exception("Team no Found");
            if (team.OwnerId != userId) throw new Exception("You are not perrmission delete");
            var pendingInvites = _context.Notifications.Where(n => n.ReferenceId == teamId &&( n.Type == NotificationType.TeamInvite || n.Type == NotificationType.ApprovalRequest));
            _context.Notifications.RemoveRange(pendingInvites);
            await _activityLogService.LogAsync(userId, ActivityType.TeamDeleted, $"Delete Team: {team.Name}", teamId: null, boardId: null);
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
               .Include(m => m.User)
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == memberId);

            if (memberToRemove == null) return false;
            if (memberToRemove.UserId == currentUserId) return false;
            _context.TeamMembers.Remove(memberToRemove);

            await _activityLogService.LogAsync(currentUserId, ActivityType.MemberRemoved,
                $"Remove Member {memberToRemove.User.FullName}({memberToRemove.User.Email}) from Team", teamId: teamId, boardId: null);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task LeaveTeamAsync(Guid teamId, Guid userId)
        {
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return;
            var member = await _context.TeamMembers
                .Include(m => m.User)
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
            if (member == null) return;
            if (team.OwnerId == userId)
            {
                throw new Exception("You are Owner, you cannot leave Team. Please Delete Team or transer you Role");
            }
            _context.TeamMembers.Remove(member);
            await _activityLogService.LogAsync(
                userId,
                ActivityType.MemberLeft,
                $"Left the Team",
                teamId: teamId, boardId: null);
            await _context.SaveChangesAsync();
        }
        //OTHER
        public async Task<(bool Success, string Message)> InviteMemberAsync(Guid teamId, string email, Guid senderId)
        {
            var userToInvite = _context.Users.FirstOrDefault(u => u.Email == email);
            if (userToInvite == null) return (false, "Email user doesn't exsist.");

            var isAlreadyMember = await _context.TeamMembers
                .AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userToInvite.Id);
            if (isAlreadyMember) return (false, "User is a member Team.");

            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return (false, "Team not found.");

            var senderRole = await GetUserRoleInTeamAsync(teamId, senderId); 

            // --- LOGIC PHÂN QUYỀN MỜI ---

            // 1. Nếu là Admin mời VÀ Team yêu cầu duyệt
            if (senderRole == TeamRole.Admin && team.IsInviteApprovalRequired)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = team.OwnerId, // Người nhận là Owner
                    SenderId = senderId,   // Người gửi là Admin
                    Type = NotificationType.ApprovalRequest, 
                    Message = $"Admin want to invite {userToInvite.FullName} ({userToInvite.Email}) join team.",
                    ReferenceId = teamId,
                    CreatedAt = DateTime.Now,
                    IsRead = false,
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
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null || notification.Type != NotificationType.ApprovalRequest)
                return false;

            if (!isApproved)
            {
                if (notification.SenderId.HasValue)
                {
                    await _notificationService.CreateInfoNotificationAsync(
                        notification.SenderId.Value, 
                        "Your member invitation request has been denied.."
                    );
                }

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
                    await _notificationService.CreateInviteNotificationAsync(
                        notification.UserId, 
                        userToInviteId,      
                        teamId,
                        team.Name
                    );

                    if (notification.SenderId.HasValue)
                    {
                        await _notificationService.CreateInfoNotificationAsync(
                            notification.SenderId.Value,
                            "The owner has approved your member invitation request.."
                        );
                    }
                }

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
                    await _activityLogService.LogAsync(senderId, ActivityType.MemberJoined, $"Welcome {newMember.User.FullName} come in Team", teamId: teamId, boardId: null);
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
        public async Task<List<TeamViewModel>> GetManagedTeamsAsync(Guid userId)
        {
            return await _context.TeamMembers
                .Include(tm => tm.Team)
                .Where(tm => tm.UserId == userId && (tm.Role == TeamRole.Owner || tm.Role == TeamRole.Admin))
                .Select(tm => new TeamViewModel
                {
                    Id = tm.Team.Id,
                    Name = tm.Team.Name
                })
                .ToListAsync();
        }
        public async Task<List<User>> GetUsersForAiAsync(Guid? teamId, Guid currentUserId)
        {
            if (teamId.HasValue)
            {
                return await _context.TeamMembers
                    .Where(tm => tm.TeamId == teamId.Value)
                    .Include(tm => tm.User)
                    .ThenInclude(u => u.Skills) 
                    .Select(tm => tm.User)
                    .ToListAsync();
            }
            else
            {
                return await _context.Users
                    .Where(u => u.Id == currentUserId)
                    .Include(u => u.Skills)
                    .ToListAsync(); 
            }
        }

        //=========SETTING============
        public async Task UpdateSettingsTeam(TeamSettingViewModel model, Guid userId)
        {
            var team = await _context.Teams.FindAsync(model.TeamId);
            if (team == null) throw new Exception();
            if (team.OwnerId != userId) throw new Exception("Only the Owner has the right to change this setting..");
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
                await _activityLogService.LogAsync(currentUserId, ActivityType.RoleUpdated, $"Change Owner Role to {targetMember.User.FullName}", teamId: teamId, boardId: null);
                return (true, "Member promoted to Owner successfully.");
            }
            targetMember.Role = newRole;
            await _context.SaveChangesAsync();
            await _notificationService.CreateInfoNotificationAsync(memberId, $"Your role in team '{targetMember.Team.Name}' has been changed to {newRole}.");
            await _activityLogService.LogAsync(currentUserId, ActivityType.RoleUpdated, $"Change {newRole} Role to {targetMember.User.FullName}", teamId: teamId, boardId: null);
            return (true, "Member role updated successfully.");
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
        public async Task<List<User>> GetTeamMembersWithSkillsAsync(Guid teamId)
        {
            return await _context.TeamMembers
                .Where(tm => tm.TeamId == teamId)
                .Include(tm => tm.User)          
                .ThenInclude(u => u.Skills)     
                .Select(tm => tm.User)          
                .ToListAsync();
        }
        public async Task<TeamAnalyticsViewModel> GetTeamAnalyticsAsync(Guid teamId,Guid userId)
        {
            var team = await _context.Teams
                 .Include(t => t.Members)
                     .ThenInclude(t => t.User)
                 .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) return null;
            var result = new TeamAnalyticsViewModel
            {
                TeamId=team.Id,
                TeamName=team.Name,
                Decription = team.Description
            };
            result.CurrentUserRole = (TeamRole)await GetUserRoleInTeamAsync(teamId, userId);
            var memberIds = team.Members.Select(m => m.UserId).ToList();
            var assignments = await _context.TaskAssignments
                .Include(a => a.Task)
                .Where(a => memberIds.Contains(a.UserId))
                .ToListAsync();

                var uniqueTasks = assignments
                     .Select(a => a.Task)
                     .DistinctBy(t => t.Id)
                     .ToList();

            // --- TÍNH TOÁN KPI TOÀN TEAM ---
            result.TotalTasks = uniqueTasks.Count;
            result.CompletedTasks = uniqueTasks.Count(t => t.Status == Models.TaskStatus.Completed);
            result.OverdueTasks = uniqueTasks.Count(t =>
                t.Status != Models.TaskStatus.Completed &&
                t.DueDate.HasValue &&
                t.DueDate.Value < DateTime.UtcNow);
            result.PendingTask = result.TotalTasks - result.CompletedTasks;

            //Tinh KPI 
            foreach (var member in team.Members)
            {
                var userAssignments = assignments.Where(a => a.UserId == member.UserId).ToList();

                var stat = new MemberPerformanceViewModel
                {
                    UserId = member.UserId,
                    FullName = member.User.FullName,
                    AvatarUrl = member.User.AvatarUrl,
                    JobTitle = member.User.JobTitle ?? "Member", // Lấy JobTitle từ Profile thật

                    AssignedCount = userAssignments.Count,
                    CompletedCount = userAssignments.Count(a => a.Task.Status == Models.TaskStatus.Completed),
                    OverdueCount = userAssignments.Count(a => a.Task.Status != Models.TaskStatus.Completed && a.Task.DueDate < DateTime.Now)
                };
                result.MemberStats.Add(stat);
            }
            result.MemberStats = result.MemberStats.OrderByDescending(x=>x.OverdueCount).ToList();

            var attentionTasks = await _context.Tasks
                .Include(t => t.Assignments).ThenInclude(a => a.User)
                // .Include(t => t.List).ThenInclude(l => l.Board) // Nếu muốn truy xuất thông tin Board sau này
                .Where(t => t.List.Board.TeamId == teamId 
                            && t.Status != Models.TaskStatus.Completed 
                            && (t.DueDate < DateTime.UtcNow || t.DueDate < DateTime.UtcNow.AddDays(1)))
                .OrderBy(t => t.DueDate)
                .Take(10)
                .ToListAsync();
            foreach (var task in attentionTasks)
            {
                result.AttentionTasks.Add(new TaskAlertViewModel
                {
                    TaskId = task.Id,
                    TaskName = task.Title,
                    DueDate = task.DueDate ?? DateTime.MaxValue,
                    Status = task.Status,
                    Assignees = task.Assignments.Select(a => new MemberPerformanceViewModel
                    {
                        UserId = a.UserId,
                        FullName = a.User.FullName,
                        AvatarUrl = a.User.AvatarUrl
                    }).ToList()
                });
            }
            return result;
        }
        public async Task<string> SendRemindAsync(Guid senderId, Guid targetUserId, Guid referenceId, string referenceName, bool isTaskReminder)
        {
            Guid? teamIdToCheck = null;

            if (isTaskReminder)
            {
                // ReferenceId chính là TaskId (Guid)
                var task = await _context.Tasks
                    .Include(t => t.List)
                        .ThenInclude(l => l.Board)
                    .FirstOrDefaultAsync(t => t.Id == referenceId);

                // Lấy TeamId từ Board (nếu Board đó thuộc về Team)
                if (task?.List?.Board?.TeamId != null)
                {
                    teamIdToCheck = task.List.Board.TeamId.Value;
                }
            }
            else
            {
                // ReferenceId chính là TeamId (Guid)
                teamIdToCheck = referenceId;
            }

            if (teamIdToCheck.HasValue)
            {
                var currentMember = await _context.TeamMembers
                    .FirstOrDefaultAsync(m => m.TeamId == teamIdToCheck.Value && m.UserId == senderId);

                if (currentMember == null ||
                   (currentMember.Role.ToString() != "Owner" && currentMember.Role.ToString() != "Admin"))
                {
                    return "Unauthorized"; 
                }
            }

            var todayCount = await _context.Notifications.CountAsync(n =>
                n.SenderId == senderId &&
                n.UserId == targetUserId &&
                n.ReferenceId == referenceId &&
                n.CreatedAt.Date == DateTime.UtcNow.Date);

            if (todayCount >= 2)
            {
                return "SpamLimitReached"; 
            }

            string messageContent = isTaskReminder
                ? $"remind you about task '{referenceName}' not Complete"
                : $"remind you about Job in team '{referenceName}'";

            await _notificationService.CreateRemindNotificationAsync(senderId, targetUserId, referenceId, referenceName, messageContent);

            return "Success";
        }
    }
}
