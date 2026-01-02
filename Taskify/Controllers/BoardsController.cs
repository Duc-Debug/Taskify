using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using Taskify.Models;
using Taskify.Services;

namespace Taskify.Controllers
{
    [Authorize]
    public class BoardsController : Controller
    {
        private readonly IBoardService _boardService;
        private readonly ITeamService _teamService;
        private readonly ITaskService _taskService;
        private readonly IActivityLogService _activitiesLogService;
        private readonly IGeminiService _geminiService;

        public BoardsController(IBoardService boardService, ITeamService teamService, ITaskService taskService, IActivityLogService activityLogService, IGeminiService geminiService)
        {
            _boardService = boardService;
            _teamService = teamService;
            _taskService = taskService;
            _activitiesLogService = activityLogService;
            _geminiService = geminiService;
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
            var board = await _boardService.GetBoardDetailsAsync(id, userId);
            if (board == null) return NotFound();
            bool hasAccess = false;
            if (board.TeamId != Guid.Empty)
            {
                var role = await _teamService.GetUserRoleInTeamAsync(board.TeamId, userId);
                if (role.HasValue) hasAccess = true;
            }
           
            if (!hasAccess && board.TeamId != Guid.Empty)
            {
                TempData["ErrorMessage"] = "ERROR SECURITY: You are not member in this Team.";
                return RedirectToAction(nameof(Index));
            }
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
                    if (model.TeamId.HasValue && model.TeamId.Value != Guid.Empty)
                    {
                        var role = await _teamService.GetUserRoleInTeamAsync(model.TeamId.Value, userId);
                        if (role == TeamRole.Member)
                        {
                            TempData["ErrorMessage"] = "Member doesn't permission to create board in team";
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    await _boardService.CreateBoardAsync(model, userId);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    return Content($"LỖI SERVER KHI TẠO: {ex.Message} - {ex.InnerException?.Message}");
                }
            }

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
                TempData["ErrorMessage"] = $"KHÔNG XÓA ĐƯỢC BOARD! Lỗi Database: {ex.Message} - {ex.InnerException?.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var model = await _boardService.GetBoardForEditAsync(id);
            if (model == null) return NotFound();
            return PartialView("_EditBoardModal", model);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(BoardEditViewModel model)
        {
            var userId = GetCurrentUserId();
            await _boardService.UpdateBoardAsync(model, userId);
            TempData["SuccessMessage"] = "Update Board Successfully!";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpGet]
        public async Task<IActionResult> CreateWithAi()
        {
            var userId = GetCurrentUserId();

            // Gọi Service lấy danh sách team (đúng chuẩn Layered Architecture)
            var authorizedTeams = await _teamService.GetManagedTeamsAsync(userId);

            var viewModel = new CreateBoardAiViewModel
            {
                IsTeamBoard = false,
                Teams = new SelectList(authorizedTeams, "Id", "Name")
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateWithAi(CreateBoardAiViewModel model)
        {
            var userId = GetCurrentUserId();

            // 1. Validate Logic UI
            if (model.IsTeamBoard && model.TeamId == null)
            {
                ModelState.AddModelError("TeamId", "Vui lòng chọn Team.");
            }

            if (!ModelState.IsValid)
            {
                var teams = await _teamService.GetManagedTeamsAsync(userId);
                model.Teams = new SelectList(teams, "Id", "Name", model.TeamId);
                return View(model);
            }

            // 2. Validate Quyền hạn (Security Check)
            if (model.IsTeamBoard && model.TeamId.HasValue)
            {
                var role = await _teamService.GetUserRoleInTeamAsync(model.TeamId.Value, userId);
                if (role != TeamRole.Owner && role != TeamRole.Admin)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền tạo bảng cho Team này.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // 3. Lấy dữ liệu cho AI (Gọi Service, không query tại đây)
            // Hàm này sẽ tự xử lý logic: nếu TeamId null -> lấy 1 user, nếu có TeamId -> lấy list member
            var participants = await _teamService.GetUsersForAiAsync(model.IsTeamBoard ? model.TeamId : null, userId);

            // 4. Gọi Gemini AI
            var aiPlan = await _geminiService.GenerateBoardPlanAsync(model.Prompt, participants);

            if (aiPlan == null)
            {
                ModelState.AddModelError("", "AI không phản hồi. Vui lòng thử lại chi tiết hơn.");
                var teams = await _teamService.GetManagedTeamsAsync(userId);
                model.Teams = new SelectList(teams, "Id", "Name", model.TeamId);
                return View(model);
            }

            // 5. Lưu vào DB (Gọi BoardService)
            Guid? finalTeamId = model.IsTeamBoard ? model.TeamId : null;
            // Lưu ý: Cần thêm hàm CreateBoardFromAiAsync vào IBoardService như đã bàn ở bước trước
            var newBoardId = await _boardService.CreateBoardFromAiAsync(aiPlan, userId, finalTeamId);

            return RedirectToAction(nameof(Details), new { id = newBoardId });
        }


        [HttpPost]
        public async Task<IActionResult> CreateList([FromBody] CreateListRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }
            var userId = GetCurrentUserId();
            var board = await _boardService.GetBoardDetailsAsync(request.BoardId, userId);
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
            var userId = GetCurrentUserId();
            var role = await _taskService.GetUserRoleInBoardAsync(request.BoardId, userId);
            if (role == TeamRole.Member)
            {
                return StatusCode(403, new { success = false, message = "Members do not have the right to change the board structure." });
            }
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
                var role = await _teamService.GetUserRoleInTeamAsync((Guid)list.Board.TeamId, userId);
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