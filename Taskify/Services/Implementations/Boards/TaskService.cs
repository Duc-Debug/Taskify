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
            var task = await _context.Tasks.Include(t => t.List).FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return;

            bool isListChanged = task.ListId != targetListId;
            string oldListName = task.List?.Title;

            // Cập nhật vị trí
            task.ListId = targetListId;
            task.Order = newPosition;

            // [LOGIC MỚI] Tự động cập nhật Status dựa trên tên List
            if (isListChanged)
            {
                var targetList = await _context.TaskLists.FindAsync(targetListId);
                if (targetList != null)
                {
                    // Chuẩn hóa tên về chữ thường để so sánh
                    var listTitle = targetList.Title.Trim().ToLower();

                    if (listTitle == "to do" || listTitle == "todo")
                    {
                        task.Status = Models.TaskStatus.Pending;
                    }
                    else if (listTitle == "done" || listTitle == "completed" || listTitle == "complete" || listTitle == "finished")
                    {
                        task.Status = Models.TaskStatus.Completed;
                    }
                    else
                    {
                        // Tất cả các tên khác (Doing, In Process, Review, Testing...) đều về InProgress
                        task.Status = Models.TaskStatus.InProgress;
                    }

                    // Ghi log
                    await LogHistoryAsync(taskId, $"Moved from <strong>{oldListName}</strong> to <strong>{targetList.Title}</strong>");
                }
            }

            _context.Tasks.Update(task);
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
                foreach (var assigneedId in model.SelectedAssigneeIds.Where(id => id != userId))
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
            var task = await _context.Tasks
                 .Include(t => t.Assignments)
                 .Include(t => t.Comments)
                 .Include(t => t.TaskHistories)
                 .FirstOrDefaultAsync(t => t.Id == taskId);
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
                .Include(t => t.Assignments).ThenInclude(a => a.User)
                .Include(t => t.List) // Lay ten List
                .FirstOrDefaultAsync(t => t.Id == taskId);

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
                .Include(t => t.List)
                .Include(t => t.Assignments).ThenInclude(a => a.User)
                .Where(t => t.Assignments.Any(a => a.UserId == userId))

                .OrderBy(t => t.Status == Models.TaskStatus.Completed) // False (0) lên trước, True (1) xuống dưới
                .ThenBy(t => t.DueDate)
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
                Status = task.Status,
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
