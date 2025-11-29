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
            var activities = await _context.TaskHistories
                .Include(h => h.TaskItem)
                .Where(h => h.TaskItem.Assignments.Any(a => a.UserId == userId))
                .OrderByDescending(h => h.Timestamp)
                .Take(5)
                .Select(h => new ActivityViewModel
                {
                    Description = $"{h.Action}: {h.TaskItem.Title}",

                    Type = h.Action.Contains("Create") ? "created" :
                        h.Action.Contains("Complete") ? "completed" :
                        h.Action.Contains("Assign") ? "asigned" : "Comment",
                    TimeAgo = CalculateTimeAgo(h.Timestamp)
                })
                .ToListAsync();

            //Ghep data vao DashboardViewModel
            var model = new DashboardViewModel
            {
                TotalAssignedTasks = totalTasks,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                MyTeams = myTeams,
               RecentActivities=activities
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
