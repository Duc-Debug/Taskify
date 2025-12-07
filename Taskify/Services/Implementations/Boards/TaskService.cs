using Taskify.Data;
using Taskify.Models;
using Microsoft.EntityFrameworkCore;

namespace Taskify.Services
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;
        public TaskService(AppDbContext context)
        {
            _context = context;
        }
        public async Task MoveTaskAsync(Guid taskId, Guid targetListId, int newPosition)
        {
           var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return;

            bool isListChanged=task.ListId != targetListId;
            string oldListName = task.List?.Title;

            //Update ListId if change column
            task.ListId = targetListId;
            //Update Order, Comming Soon: Adjust orders of other tasks in the list accordingly
            task.Order =newPosition;

            _context.Tasks.Update(task);
            //Ghi log
            if (isListChanged)
            {
                var newList = await _context.TaskLists.FindAsync(targetListId);
                await LogHistoryAsync(taskId, $"Moved from {oldListName} to {newList?.Title}");
            }
            await _context.SaveChangesAsync();
        }
        public async Task<TaskItem> CreateTaskAsync(TaskCreateViewModel model, Guid userId)
        {
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = model.Title,
                Description = model.Description,
                DueDate = model.DueDate,
                Priority = model.Priority,
                ListId = model.ListId,
                Order = 999,
                Assignments = new List<TaskAssignment>()
            };
            //Add User in Assignments
            task.Assignments.Add(new TaskAssignment
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                UserId = userId

            });

            //2. Assign them cac thanh vien neu co chon tu form
            if (model.SelectedAssigneeIds != null)
            {
                foreach(var assigneedId in model.SelectedAssigneeIds.Where(id => id != userId))
                {
                    task.Assignments.Add(new TaskAssignment
                    {
                        Id = Guid.NewGuid(),
                        TaskId = task.Id,
                        UserId = assigneedId
                    });
                } 
            }
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            await LogHistoryAsync(task.Id, "Created task");
            return task;
        }
        public async Task DeleteTaskAsync(Guid taskId)
        {
           var task= await _context.Tasks
                .Include(t=>t.Assignments)
                .Include(t=>t.Comments)
                .Include(t=>t.TaskHistories)
                .FirstOrDefaultAsync(t=> t.Id == taskId);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<TaskItem> GetTaskByIdAsync(Guid taskId)
        {
            return await _context.Tasks
                .Include(t => t.Assignments)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }
        public async Task<TaskDetailsViewModel> GetTaskDetailsAsync(Guid taskId)
        {
            var task = await _context.Tasks
                .Include(t=> t.Assignments).ThenInclude(a=>a.User)
                .Include(t=>t.List) // Lay ten List
                .FirstOrDefaultAsync(t=> t.Id == taskId);

            if (task == null) return null;

            var histories = await _context.TaskHistories
                .Where(h => h.TaskItemId == taskId)
                .OrderByDescending(h => h.Timestamp)
                .Take(10)
                .ToListAsync();

            //Map tu entity sang VM
            return new TaskDetailsViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                DueDate = task.DueDate,
                ListId = task.ListId,
                ListName = task.List?.Title,
                CreatedAt = DateTime.Now, // Vi du thoi, neu co DB lay sau

                //Map assignees
                Assignees = task.Assignments.Select(a => new MemberViewModel
                {
                    Id = a.UserId,
                    FullName = a.User.FullName,
                    AvatarUrl = a.User.AvatarUrl,
                    Initials = a.User.FullName?.Substring(0, 1) ?? "U"
                }).ToList(),


                Activities = histories.Select(h => new TaskHistoryViewModel
                {
                    Action = h.Action,
                    Timestamp = h.Timestamp,
                    UserFullName = "Unkown"
                }).ToList()
            };
        }
        public async Task<List<TaskDetailsViewModel>> GetTasksByUserIdAsync(Guid userId)
        {
            var tasks = await _context.Tasks
                .Include(t=>t.List)
                .Include(t=>t.Assignments).ThenInclude(a=>a.User)
                //Loc cac task ma user dc assign 
                .Where(t=> t.Assignments.Any(a=>a.UserId==userId))
                .OrderByDescending(t=>t.DueDate)
                .ToListAsync();

            //Map sang VM
            return tasks.Select(task => new TaskDetailsViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                DueDate = task.DueDate,
                ListId = task.ListId,
                ListName = task.List?.Title,
                //Chua co DB CreateAt nen lay DateTime Now
                CreatedAt = DateTime.Now,
                Assignees = task.Assignments.Select(a => new MemberViewModel
                {
                    Id = a.UserId,
                    FullName = a.User.FullName,
                    AvatarUrl = a.User.AvatarUrl,
                    Initials = !string.IsNullOrEmpty(a.User.FullName) ? a.User.FullName.Substring(0, 1) : "U"
                }).ToList()
            }).ToList();
        }
        private async Task LogHistoryAsync(Guid taskId, string action)
        {
            var history = new TaskHistory
            {
                TaskItemId = taskId,
                Action = action,
                Timestamp = DateTime.Now
            };
            _context.TaskHistories.Add(history);
            // SaveChangesAsync sẽ được gọi ở cuối các hàm Public, 
            // nhưng gọi luôn ở đây để đảm bảo History được lưu ngay lập tức
            await _context.SaveChangesAsync();
        }
    }
}
