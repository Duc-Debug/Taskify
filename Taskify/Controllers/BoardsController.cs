using Microsoft.AspNetCore.Mvc;
using Taskify.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Taskify.Controllers
{
    [Authorize]
    public class BoardsController : Controller
    {
        private readonly IBoardService _boardService;

        public BoardsController(IBoardService boardService)
        {
            _boardService = boardService;
        }
        private Guid GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }
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

        [HttpPost]
        public async Task<IActionResult> Create(string name)
        {
           var userId = GetCurrentUserId();
            if(userId != Guid.Empty && !string.IsNullOrWhiteSpace(name))
            {
                await _boardService.CreateBoardAsync(name, userId);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}