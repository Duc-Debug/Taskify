using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Collections.Generic;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Services
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;
        public TaskService(AppDbContext context, INotificationService notificationService, IActivityLogService activityLogService)
        {
            _context = context;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }
        public async Task MoveTaskAsync(Guid taskId, Guid targetListId, int newPosition,Guid userId)
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

                    if (listTitle == "to do" || listTitle == "todo" || listTitle == "backlog")
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
                    await LogHistoryAsync(taskId, $"Moved from {oldListName}  to {targetList.Title}");
                    var board = await _context.Boards.FindAsync(task.List.BoardId);
                  //  var userName = await _context.Users.FindAsync(userId);
                    await _activityLogService.LogAsync(userId, ActivityType.TaskMoved,
                        $"Move task from {oldListName} to List {targetList.Title}", teamId: board?.TeamId, boardId: board.Id);
                }
            }

            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
        }

        public async Task<TaskItem> CreateTaskAsync(TaskCreateViewModel model, Guid userId)
        {
            var status = Models.TaskStatus.Pending;
            var list = await _context.TaskLists.FindAsync(model.ListId);
            if (list != null)
            {
                var listTitle = list.Title.Trim().ToLower();
                if (listTitle == "done" || listTitle == "completed" || listTitle == "complete" || listTitle == "finished")
                {
                    status = Models.TaskStatus.Completed;
                }
                else if (listTitle == "to do" || listTitle == "todo" || listTitle == "backlog")
                {
                    status = Models.TaskStatus.Pending;
                }
                else
                {
                    status = Models.TaskStatus.InProgress;
                }
            }
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = model.Title,
                Description = model.Description,
                DueDate = model.DueDate,
                Priority = model.Priority,
                ListId = model.ListId,
                CreatorId = userId,//
                Status = status,
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

            var board = await _context.Boards.FindAsync(model.BoardId);
            await _activityLogService.LogAsync(userId, ActivityType.TaskCreated,
                $"Create task: {task.Title} in List {list.Title}", teamId: board?.TeamId, boardId: board.Id);
            return task;
        }
        public async Task<(bool Success, string Message)> DeleteTaskAsync(Guid taskId, Guid userId)
        {
            var task = await _context.Tasks
                 .Include(t => t.Assignments)
                 .Include(t => t.Comments)
                 .Include(t => t.TaskHistories)
                 .Include(t => t.List)
                 .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return (false, "Don't have Task");
            var boardId = task.List.BoardId;
            var userRole = await GetUserRoleInBoardAsync(boardId, userId);
            if (userRole == TeamRole.Member && task.CreatorId != userId)
            {
                return (false, "You don't have permission to delete this task");
            }
            _context.Tasks.Remove(task);
            var board = await _context.Boards.FindAsync(boardId);
            await _activityLogService.LogAsync(userId, ActivityType.TaskDeleted,
                $"Delete task: {task.Title}", teamId: board?.TeamId, boardId: boardId);
            await _context.SaveChangesAsync();
            return (true, "Delete task successfully");

        }
        public async Task<TaskItem> GetTaskByIdAsync(Guid taskId)
        {
            return await _context.Tasks
                .Include(t => t.Assignments)
                .ThenInclude(t => t.User)
                .Include(t=>t.List)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }
        public async Task<TaskDetailsViewModel> GetTaskDetailsAsync(Guid taskId)
        {
            var task = await _context.Tasks
                .Include(t => t.Assignments).ThenInclude(a => a.User)
                .Include(t => t.List).ThenInclude(l=>l.Board) // Lay ten List
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return null;

            var histories = await _context.TaskHistories
                .Where(h => h.TaskItemId == taskId)
                .OrderByDescending(h => h.Timestamp)
                .Take(10)
                .ToListAsync();
            var teamId = task.List.Board.TeamId;
            var allTeamMembers = await _context.TeamMembers
                .Include(tm => tm.User)
                .Where(tm => tm.TeamId == teamId)
                .Select(tm => new MemberViewModel
                {
                    Id = tm.UserId,
                    FullName = tm.User.FullName,
                    AvatarUrl = tm.User.AvatarUrl,
                    Initials = tm.User.FullName.Substring(0, 1) ?? "U"

                }).ToListAsync();
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
                TeamMembers = allTeamMembers,

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

        public async Task<(bool Success, string Message)> UpdateTaskAsync(TaskEditViewModel model, Guid userId, TeamRole? userRole)
        {
            var task = await _context.Tasks
                .Include(t => t.Assignments)
                .Include(t=>t.List)
                .FirstOrDefaultAsync(t => t.Id == model.Id);
            if (task == null) return (false, "Don't have Task");

            if (userRole == TeamRole.Member && task.CreatorId != userId)
            {
                return (false, "You don't have permission to update this task");
            }
            task.Title = model.Title;
            task.Description = model.Description;
            task.Priority = model.Priority;
            task.DueDate = model.DueDate;
            _context.TaskAssignments.RemoveRange(task.Assignments);
            if (model.SelectedAssigneeIds != null)
            {
                foreach (var assigneeId in model.SelectedAssigneeIds)
                {
                    task.Assignments.Add(new TaskAssignment
                    {
                        Id = Guid.NewGuid(),
                        TaskId = task.Id,
                        UserId = assigneeId
                    });
                }
            }

            _context.Tasks.Update(task);
            await LogHistoryAsync(task.Id, "Updated task details");
            var board = await _context.Boards.FindAsync(task.List.BoardId);
            await _activityLogService.LogAsync(userId, ActivityType.TaskUpdated,
                $"Update task: {task.Title}", teamId: board?.TeamId, boardId: task.List.BoardId);
            await _context.SaveChangesAsync();
            return (true, "Update task successfully");
        }

        public async Task<TaskEditViewModel> GetTaskForEditAsync(Guid taskId)
        {
            var task = await _context.Tasks
                 .Include(t => t.Assignments)
                 .Include(t => t.List)
                 .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return null;
            var assignedUserIds = task.Assignments.Select(a => a.UserId).ToList();
            return new TaskEditViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                DueDate = task.DueDate,
                BoardId = task.List.BoardId,
                ListId = task.ListId,
                SelectedAssigneeIds = assignedUserIds,
                //AvailableMembers = await _context.BoardMembers
                //    .Where(bm => bm.BoardId == task.List.BoardId)
                //    .Select(bm => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                //    {
                //        Value = bm.UserId.ToString(),
                //        Text = bm.User.FullName
                //    }).ToListAsync()
            };
        }
        public async Task<TeamRole?> GetUserRoleInBoardAsync(Guid boardId, Guid userId)
        {
            // Truy vấn: Từ Board -> tìm Team -> tìm Member -> lấy Role
            var role = await _context.Boards
                .Where(b => b.Id == boardId)
                .SelectMany(b => b.Team.Members)
                .Where(tm => tm.UserId == userId)
                .Select(tm => (TeamRole?)tm.Role)
                .FirstOrDefaultAsync();

            return role;
        }
        public async Task AssignMemberASync(Guid taskId, Guid userId)
        {
            var exists = await _context.TaskAssignments
                .AnyAsync(ta => ta.Id == taskId && ta.UserId == userId);
            if (!exists)
            {
                var assignment = new TaskAssignment
                {
                    Id = Guid.NewGuid(),
                    TaskId = taskId,
                    UserId = userId,
                };
                var taskName = await _context.Tasks.Where(i=>i.Id== taskId)
                    .Select(name=>name.Title)
                    .FirstOrDefaultAsync();

                _context.TaskAssignments.Add(assignment);
                await _context.SaveChangesAsync();
                await _notificationService.CreateInfoNotificationAsync(userId, $"You are assign a new task({taskName})");
            }
        }
        public async Task RemoveMemberAsync(Guid taskId, Guid userId)
        {
            var assignment = await _context.TaskAssignments
                .FirstOrDefaultAsync(ta => ta.TaskId == taskId && ta.UserId == userId);
            if (assignment != null)
            {
                _context.TaskAssignments.Remove(assignment);
                await _context.SaveChangesAsync();
            }
        }
    }
}
