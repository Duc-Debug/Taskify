using Microsoft.AspNetCore.Mvc;
using Taskify.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Taskify.Models;

namespace Taskify.Controllers
{
    [Authorize]
    public class BoardsController : Controller
    {
        private readonly IBoardService _boardService;
        private readonly ITeamService _teamService;

        public BoardsController(IBoardService boardService, ITeamService teamService)
        {
            _boardService = boardService;
            _teamService = teamService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserTeams()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var teams = await _teamService.GetTeamsByUserIdAsync(userId);
            return Json(teams.Select(t => new { id = t.Id, name = t.Name }));
        }

        private Guid GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdString, out var userId)) return userId;
            return Guid.Empty;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Account");
            var boards = await _boardService.GetBoardsByUserIdAsync(userId);
            return View(boards);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var userId = GetCurrentUserId();
            var board = await _boardService.GetBoardDetailsAsync(id,userId);
            if (board == null) return NotFound();
            return View(board);
        }

        [HttpPost]
        public async Task<IActionResult> Create(BoardCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = GetCurrentUserId();
                    await _boardService.CreateBoardAsync(model, userId);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    return Content($"LỖI SERVER KHI TẠO: {ex.Message} - {ex.InnerException?.Message}");
                }
            }

            // Nếu dữ liệu sai, in ra lỗi cụ thể
            var errors = string.Join(" | ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            return Content($"LỖI DỮ LIỆU (VALIDATION): {errors}");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _boardService.DeleteBoardAsync(id, userId);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return Content($"KHÔNG XÓA ĐƯỢC BOARD! Lỗi Database: {ex.Message} - {ex.InnerException?.Message}");
            }
        }
        [HttpPost]
        public async Task<IActionResult> Edit(BoardEditViewModel model)
        {
            var userId = GetCurrentUserId();
            await _boardService.UpdateBoardAsync(model, userId);
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        [HttpPost]
        public async Task<IActionResult> CreateList([FromBody] CreateListRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }
            var userId = GetCurrentUserId();
            var board = await _boardService.GetBoardDetailsAsync(request.BoardId,userId);
            if (board == null) return NotFound("Board don't exsist.");
            var memberRole = await _teamService.GetUserRoleInTeamAsync(board.TeamId, userId);
            if (memberRole == TeamRole.Member) return StatusCode(403, "You don't have permission to add list.");
            await _boardService.CreateListAsync(request.BoardId, request.Title, userId);
            return Ok(new { success = true });
        }
        [HttpPost]
        public async Task<IActionResult> MoveList([FromBody] MoveListRequest request)
        {
            if (request == null) return BadRequest("Dữ liệu không hợp lệ.");
            await _boardService.UpdateListOrderAsync(request.BoardId, request.ListId, request.NewIndex);
            return Ok(new { success = true });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteList(Guid listId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var list = await _boardService.GetListByIdAsync(listId);
                if (list == null) return NotFound("List don't exsist.");
                var role = await _teamService.GetUserRoleInTeamAsync(list.Board.TeamId, userId);
                if (role == TeamRole.Member) return StatusCode(403, "You don't have permission to delete list.");
                await _boardService.DeleteListAsync(listId, userId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException != null ?
                            $"{ex.Message} -> {ex.InnerException.Message}" :
                            ex.Message;

                return BadRequest(new { success = false, message = errorMessage });
            }
        }
        public class CreateListRequest
        {
            public Guid BoardId { get; set; }
            public string Title { get; set; }
        }
        public class MoveListRequest
        {
            public Guid BoardId { get; set; }
            public Guid ListId { get; set; }
            public int NewIndex { get; set; }
        }
    }
}