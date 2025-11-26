using Microsoft.AspNetCore.Mvc;
using Taskify.Services;

namespace Taskify.Controllers
{
    public class BoardsController : Controller
    {
        private readonly IBoardService _boardService;

        // [CẦN SỬA] Bạn hãy vào SQL Server, copy ID của 1 user bất kỳ dán vào đây để test
        private readonly Guid HARD_CODED_USER_ID = Guid.Parse("COPY-GUID-CUA-USER-VAO-DAY");

        public BoardsController(IBoardService boardService)
        {
            _boardService = boardService;
        }

        public async Task<IActionResult> Index()
        {
            var boards = await _boardService.GetBoardsByUserIdAsync(HARD_CODED_USER_ID);
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
            await _boardService.CreateBoardAsync(name, HARD_CODED_USER_ID);
            return RedirectToAction(nameof(Index));
        }
    }
}