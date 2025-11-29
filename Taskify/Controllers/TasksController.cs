using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Taskify.Models;
using Taskify.Services;

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
            return Ok(new {success = true});
        }
        [HttpPost]
        public async Task<IActionResult> Create (TaskCreateViewModel model)
        {
            //Logic tao task tu Modal
            if (ModelState.IsValid)
            {
                //Lay UserId tu Cookie cu
                var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                await _taskService.CreateTaskAsync(model, userId);
                
                //Quay lai trang Board cu
                return RedirectToAction("Details", "Boards", new { id = model.BoardId });
               
            }
            //
            return RedirectToAction("Details", "Boards", new { id = model.BoardId });
        }
        [HttpPost]
        public async Task<IActionResult> QuickCreate(TaskCreateViewModel model)
        {
            // Logic tương tự Create, có thể xử lý thêm Category mặc định nếu cần
            return await Create(model);
        }
        public async Task<IActionResult> Delete(Guid id)
        {
            await _taskService.DeleteTaskAsync(id);
            return Ok(new { success = true });
        }
        //Class DTO(Data Transfer Object) nhan du lieu tu Js
        public class MoveTaskRequest
        {
            public Guid TaskId { get; set; }
            public Guid TargetListId { get; set; }
            public int NewPosition { get; set; }
        }

    }
}
