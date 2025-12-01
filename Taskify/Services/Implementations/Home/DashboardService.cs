using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Services
{
    public class DashboardService:IDashboardService
    {
        private readonly AppDbContext _context;
        public DashboardService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<DashboardViewModel> GetDashboardDataAsync(Guid userId)
        {
            var userTasksQuery = _context.TaskAssignments
                .Include(ta => ta.Task)
                .Where(ta => ta.UserId == userId);
            var totalTasks = await userTasksQuery.CountAsync();
            var completedTasks = await userTasksQuery.CountAsync(ta => ta.Task.Status == Models.TaskStatus.Completed);
              
            var pendingTasks = totalTasks - completedTasks;
            // 2. Lấy Board Cá Nhân (MỚI THÊM)
            var personalBoards = await _context.Boards
                .Where(b => b.OwnerId == userId && b.TeamId == null) // Board của mình & không thuộc team
                .Select(b => new BoardSummaryViewModel
                {
                    Id = b.Id,
                    Name = b.Name
                })
                .ToListAsync();
            //Lay ds my teams
            var myTeams = await _context.TeamMembers
                .Where(tm => tm.UserId == userId)
                .Include(tm => tm.Team)
                .Select(tm => new TeamSummaryViewModel
                {
                  Id = tm.Team.Id,
                    Name = tm.Team.Name,
                    //Count members
                    MemberCount = _context.TeamMembers.Count(tmm => tmm.TeamId == tm.Team.Id),
                    Boards = tm.Team.Boards.Select(b => new BoardSummaryViewModel
                    {
                        Id = b.Id,
                        Name = b.Name
                    }).ToList()
                })
                .ToListAsync();
            //Lay 5 hd gan nhat
            var activityList = new List<ActivityViewModel>();

            // A. Lấy lịch sử Task (50 items gần nhất để lọc)
            var taskActivities = await _context.TaskHistories
                .Include(h => h.TaskItem).ThenInclude(t => t.Assignments)
                .Where(h => h.TaskItem.Assignments.Any(a => a.UserId == userId)) // Task liên quan đến mình
                .OrderByDescending(h => h.Timestamp)
                .Take(10)
                .Select(h => new
                {
                    Description = $"{h.Action}: <strong>{h.TaskItem.Title}</strong>",
                    Type = h.Action.Contains("Create") ? "created" :
                           h.Action.Contains("Complete") ? "completed" : "comment",
                    Time = h.Timestamp
                }).ToListAsync();

            // B. Lấy lịch sử Tạo Board (Board do mình tạo)
            var boardActivities = await _context.Boards
                .Where(b => b.OwnerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new
                {
                    Description = $"You created board <strong>{b.Name}</strong>",
                    Type = "created", // Dùng icon màu xanh dương
                    Time = b.CreatedAt
                }).ToListAsync();

            // C. Lấy lịch sử Tham gia Team (Team mình mới vào)
            var teamActivities = await _context.TeamMembers
                .Include(tm => tm.Team)
                .Where(tm => tm.UserId == userId)
                .OrderByDescending(tm => tm.JoinedDate)
                .Take(5)
                .Select(tm => new
                {
                    Description = $"You joined team <strong>{tm.Team.Name}</strong>",
                    Type = "assigned", // Dùng icon màu xanh lá/tím
                    Time = tm.JoinedDate
                }).ToListAsync();

            // D. Gộp tất cả lại -> Sắp xếp -> Lấy 10 cái mới nhất
            var combined = taskActivities
                .Concat(boardActivities)
                .Concat(teamActivities)
                .OrderByDescending(x => x.Time)
                .Take(10)
                .Select(x => new ActivityViewModel
                {
                    Description = x.Description,
                    Type = x.Type,
                    TimeAgo = CalculateTimeAgo(x.Time)
                })
                .ToList();

            //Ghep data vao DashboardViewModel
            var model = new DashboardViewModel
            {
                TotalAssignedTasks = totalTasks,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                PersonalBoards = personalBoards,
                MyTeams = myTeams,
               RecentActivities=combined
            };
            return model;
        }
        private static string CalculateTimeAgo(DateTime timestamp)
        {
            var timeSpan = DateTime.Now - timestamp;
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
            return $"{(int)timeSpan.TotalDays}d ago";
        }
    }
}
