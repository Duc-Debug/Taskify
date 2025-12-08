using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Taskify.Data;
using Taskify.Models;
namespace Taskify.ViewComponents
{
    public class SidebarTaskCountViewComponent: ViewComponent
    {
        private readonly AppDbContext _context;
        public SidebarTaskCountViewComponent(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdString = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return View(0);
            }
            //Dem Task ma Userdc giao
            var count = await _context.TaskAssignments
                .Include(ta => ta.Task)
                .Where(ta=> ta.UserId ==userId
               && ta.Task.Status != Models.TaskStatus.Completed
                             && ta.Task.Status != Models.TaskStatus.Cancelled)
                .CountAsync();
            return View(count);
        }

    }
}
