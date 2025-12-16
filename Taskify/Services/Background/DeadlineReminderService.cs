
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Services.Background
{
    public class DeadlineReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeadlineReminderService> _logger;
        public DeadlineReminderService(IServiceProvider serviceProvider, ILogger<DeadlineReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Deadline Reminder Service is starting");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckDeadlineAndNotifyAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while find Deadline");
                }
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
        private async Task CheckDeadlineAndNotifyAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var today = DateTime.Today;
                var daysToCheck = new List<int> { 1, 3, 5, 7 };
                foreach(var days in daysToCheck)
                {
                    var targetDate = today.AddDays(days);
                    var tasks = await context.Tasks
                        .Include(t => t.Assignments)
                        .Where(t => t.DueDate.HasValue
                                    && t.DueDate.Value.Date == targetDate
                                    && t.Status != Taskify.Models.TaskStatus.Completed)
                        .ToListAsync();
                    if (tasks.Any()) _logger.LogInformation($"Find{tasks.Count} task nearly {days} days deadline");

                    foreach(var task in tasks)
                    {
                        foreach(var assignment in task.Assignments)
                        {
                            bool alreadyNotified = await context.Notifications.AnyAsync(n =>
                                n.UserId == assignment.UserId &&
                                n.ReferenceId == task.Id &&
                                n.Type == Models.NotificationType.Info &&
                                n.CreatedAt.Date == today);
                            if (!alreadyNotified)
                            {
                                var notif = new Notification
                                {
                                    Id = Guid.NewGuid(),
                                    UserId = assignment.UserId,
                                    SenderId = null,
                                    Type=NotificationType.Info,
                                    Message=$"Deadline: Task '{task.Title}' only {days} deadline({task.DueDate:dd/MM}).",
                                    CreatedAt=DateTime.Now,
                                    IsRead=false,
                                    ReferenceId=task.Id
                                };
                                context.Notifications.Add(notif);
                            }
                        }
                    }
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
