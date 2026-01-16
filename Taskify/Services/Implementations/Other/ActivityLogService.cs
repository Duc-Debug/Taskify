using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Services.Implementations
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly AppDbContext _context;
        public ActivityLogService(AppDbContext context)
        {
            _context = context;
        }
        public async Task LogAsync(Guid actorId, ActivityType type, string content, Guid? teamId = null, Guid? boardId = null)
        {
            var log = new ActivityLog
            {
                UserId = actorId,
                Type = type,
                Content = content,
                TeamId = teamId,
                BoardId = boardId,
                CreatedAt = DateTime.Now,
            };
            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        public async Task<List<ActivityLog>> GetTeamActivitesAsync(Guid teamId, int count = 20)
        {
            return await _context.ActivityLogs
                .Include(a => a.User)
                .Where(a => a.TeamId == teamId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();

        }
        public async Task<List<ActivityLog>> GetBoardActivitiesAsync(Guid boardId, int count = 50)
        {
            return await _context.ActivityLogs
                .Include(a => a.User)
                .Where(a => a.BoardId == boardId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
        public async Task CleanupOldLogsASync()
        {
            var taskRetentionDate = DateTime.Now.AddDays(-60);
            var otherRetentionDate = DateTime.Now.AddMonths(-6);

            var taskTypes = new[]
            {
                ActivityType.TaskCreated,
                ActivityType.TaskCompleted,
                ActivityType.TaskUpdated,
                ActivityType.TaskMoved,
                ActivityType.TaskDeleted
            };

            var oldTaskLogs = _context.ActivityLogs
                .Where(a => taskTypes.Contains(a.Type) && a.CreatedAt < taskRetentionDate);
            _context.ActivityLogs.RemoveRange(oldTaskLogs);

            var oldOtherLogs = _context.ActivityLogs
                .Where(a => !taskTypes.Contains(a.Type) && a.CreatedAt < otherRetentionDate);
            _context.ActivityLogs.RemoveRange(oldOtherLogs);
            await _context.SaveChangesAsync();
        }
    }
}
