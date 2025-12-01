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
            var board = await _boardService.GetBoardDetailsAsync(id);
            if (board == null) return NotFound();
            return View(board);
        }

        // --- SỬA PHẦN CREATE ĐỂ HIỆN LỖI ---
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

        // --- SỬA PHẦN DELETE ĐỂ HIỆN LỖI ---
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
    }
}