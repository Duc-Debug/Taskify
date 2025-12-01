using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Services
{
    public class TeamService : ITeamService
    {
        public AppDbContext _context;
        public TeamService(AppDbContext context)
        {
            _context = context;
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
                    Id = tm.Id,
                    FullName = tm.User.FullName,
                    Email = tm.User.Email,
                    AvatarUrl = tm.User.AvatarUrl,
                    Initials = tm.User.FullName?.Substring(0, 1) ?? "U",
                    IsOwner = tm.Role == TeamRole.Owner,
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
    }
}
