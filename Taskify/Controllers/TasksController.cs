using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Taskify.Models;
using Taskify.Services;
using System.Security.Claims;

namespace Taskify.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ITaskService _taskService;
        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }
        // Cho kieu Kanban Board
        //API nhan lenh di chuyen (Goi bang AJAX)
        [HttpPost]
        public async Task<IActionResult> Move([FromBody] MoveTaskRequest request)
        {
            if (request == null) return BadRequest();
            await _taskService.MoveTaskAsync(request.TaskId, request.TargetListId, request.NewPosition);
            return Ok(new { success = true });
        }
        [HttpPost]
        public async Task<IActionResult> Create(TaskCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                await _taskService.CreateTaskAsync(model, userId);
                return RedirectToAction("Details", "Boards", new { id = model.BoardId });
            }
            // Tạm thời cứ để redirect để trải nghiệm mượt mà
            return RedirectToAction("Details", "Boards", new { id = model.BoardId });
        }
        [HttpPost]
        public async Task<IActionResult> QuickCreate(TaskCreateViewModel model)
        {
            // Logic tương tự Create, có thể xử lý thêm Category mặc định nếu cần
            return await Create(model);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
               var result= await _taskService.DeleteTaskAsync(id,userId);
                if (result.Success) return Ok(new { success = true, message = result.Message });
                return BadRequest(result.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDetails(Guid id)
        {
            var viewModel = await _taskService.GetTaskDetailsAsync(id);
            if (viewModel == null) return NotFound();

            return PartialView("_DetailsModal", viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> Index(string filter = "all")
        {
            // Lay User
            var userIdstring = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdstring)) return RedirectToAction("Login", "Account");
            var userId = Guid.Parse(userIdstring);

            ViewBag.CurrentFilter = filter;
            //Lay DL tu Service
            var tasks = await _taskService.GetTasksByUserIdAsync(userId);
            //Tinh Stats 
            ViewBag.TotalTasks = tasks.Count;
            ViewBag.PendingTasks = tasks.Count(t => t.Status == Models.TaskStatus.Pending);
            ViewBag.CompletedTasks = tasks.Count(t => t.Status == Models.TaskStatus.Completed);
            ViewBag.InProgressTasks = tasks.Count(t => t.Status == Models.TaskStatus.InProgress);
            ViewBag.OverdueTasks = tasks.Count(t => t.DueDate.HasValue 
                                                    && t.DueDate.Value.Date < DateTime.Now.Date 
                                                    && t.Status != Models.TaskStatus.Completed);
            
            //Filter Tasks
            switch (filter)
            {
                case "today":
                    tasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.Today).ToList();
                    break;
                case "overdue":
                    tasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date < DateTime.Now.Date && t.Status != Models.TaskStatus.Completed).ToList();
                    break;
            }
            return View(tasks);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var viewModel = await _taskService.GetTaskForEditAsync(id);
            if (viewModel == null) return NotFound();
          
            return PartialView("_EditModal", viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(TaskEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var userRole = await _taskService.GetUserRoleInBoardAsync(model.BoardId, userId);
                var result=  await _taskService.UpdateTaskAsync(model, userId,userRole);
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Details", "Boards", new { id = model.BoardId });
                }
                TempData["SuccessMessage"] = "Task updated successfully."; 
                return RedirectToAction("Details", "Boards", new { id = model.BoardId });
            }
            var referer = Request.Headers["Referer"].ToString();
            return Redirect(string.IsNullOrEmpty(referer) ? "/Tasks" : referer);
        }
       
        
    }
//Class DTO(Data Transfer Object) nhan du lieu tu Js
        public class MoveTaskRequest
        {
            public Guid TaskId { get; set; }
            public Guid TargetListId { get; set; }
            public int NewPosition { get; set; }
        }
}
