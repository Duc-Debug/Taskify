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

            //Update ListId if change column
            task.ListId = targetListId;

            //Update Order, Comming Soon: Adjust orders of other tasks in the list accordingly
            task.Order =newPosition;

            // Co the them thuoc tinh "Type" 
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
                Order = 999
            };
            //Add User in Assignments
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }
        public async Task DeleteTaskAsync(Guid taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
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
    }
}
