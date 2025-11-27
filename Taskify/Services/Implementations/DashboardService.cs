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

            //Ghep data vao DashboardViewModel
            var model = new DashboardViewModel
            {
                TotalAssignedTasks = totalTasks,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                MyTeams = myTeams
                //RecentAcctivities Lam sau
            };
            return model;
        }
    }
}
