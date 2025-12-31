using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using Taskify.Models;
using Taskify.Services;

namespace Taskify.Controllers
{
    [Authorize]
    public class TeamsController : Controller
    {
        private readonly ITeamService _teamService;

        public TeamsController(ITeamService teamService)
        {
            _teamService = teamService;
        }
        private Guid GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(id);
        }

        public async Task<IActionResult> Index()
        {
            var teams = await _teamService.GetTeamsByUserIdAsync(GetCurrentUserId());
            
            return View(teams);
        }
        [HttpPost]
        public async Task<IActionResult> Create(TeamCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _teamService.CreateTeamAsync(model, GetCurrentUserId());
                return RedirectToAction(nameof(Index));
            }
            // Để giữ trải nghiệm tốt hơn, sau này ta sẽ dùng AJAX, còn hiện tại dùng Redirect là an toàn nhất.
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var team = await _teamService.GetTeamDetailsAsync(id, userId);

            if (team == null) return NotFound();

            return View(team);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var model = await _teamService.GetTeamForEditAsync(id);
            if (model == null) return NotFound();
            return PartialView("_EditModal", model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TeamEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = GetCurrentUserId();
                    await _teamService.UpdateTeamAsync(model, userId);

                    TempData["SuccessMessage"] = "Team update successful!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                }
            }
            // Nếu lỗi thì quay về Index
            return RedirectToAction(nameof(Index));
        }

        // --- DELETE TEAM ---

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _teamService.DeleteTeamAsync(id, userId);

                TempData["SuccessMessage"] = "Team has been successfully deleted..";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> RemoveMember([FromBody] RemoveMemberRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _teamService.RemoveMemberAsync(request.TeamId, request.MemberId, userId);

            if (result) return Ok(new { success = true });
            return BadRequest(new { success = false, message = "Failed to remove member" });
        }
        [HttpPost]
        public async Task<IActionResult> Leave(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _teamService.LeaveTeamAsync(id, userId);
                TempData["SuccessMessage"] = " You are leave Team successfully";
                return RedirectToAction("Index", "Dashboard");
            }
            catch(Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }
        [HttpPost]
        public async Task<IActionResult> InviteMember(Guid teamId, string email)
        {
           var currentUserId = GetCurrentUserId();
            var result = await _teamService.InviteMemberAsync(teamId, email, currentUserId);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"]= result.Message;
            }
            return RedirectToAction("Details", new { id = teamId });
        }
        [HttpPost]
        public async Task<IActionResult> RespondInvite(Guid notificationId,bool isAccepted)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _teamService.RespondInvitationAsync(notificationId, currentUserId, isAccepted);

            if (!result.Success) return BadRequest(result.Message);

            return Ok(new { message = result.Message });
        }
        [HttpPost]
        public async Task<IActionResult> ChangeMemberRole(Guid teamId, Guid memberId, TeamRole newRole)
        {
            if (newRole == 0)
            {
                TempData["ErrorMessage"] = "Lỗi: No Role received from the interface. (Value = 0)";
                return RedirectToAction("Details", new { id = teamId });
            }
            var currentUserId = GetCurrentUserId();
            var roleEnum = (TeamRole)newRole;
            var result = await _teamService.ChangeMemberRoleAsync(teamId, memberId, roleEnum, currentUserId);
            if(result.Success)
            {
               TempData["SuccessMessage"] = result.Message;
            }
            else
            {
               TempData["ErrorMessage"] = result.Message;
            }
            return RedirectToAction("Details", new { id = teamId });
        }
        [HttpPost]
        public async Task<IActionResult> Settings(TeamSettingViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _teamService.UpdateSettingsTeam(model, userId);
                TempData["SuccessMessage"] = "Update Setting Team sucessffully";
            }
            catch(Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id = model.TeamId });
        }
        [HttpGet]
        public async Task<IActionResult> Analytics(Guid id)
        {
            var userId = GetCurrentUserId();
            var analytics = await _teamService.GetTeamAnalyticsAsync(id,userId);
            if (analytics == null) return NotFound();

            return View(analytics);
        }
        [HttpPost]
        public async Task<IActionResult> SendReminder(Guid targetUserId, Guid referenceId, string referenceName, string type)
        {
            // 1. Lấy ID người đang đăng nhập (Sender)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            var currentUserId = Guid.Parse(userIdClaim.Value);

            // 2. Xác định loại Reminder
            bool isTask = type == "Task";

            // 3. Gọi Service (Logic check quyền & check spam nằm hết ở đây)
            // Lưu ý: referenceId ở đây truyền vào là Guid chuẩn, không cần parse
            var result = await _teamService.SendRemindAsync(currentUserId, targetUserId, referenceId, referenceName, isTask);

            // 4. Phản hồi về Client
            if (result == "Success")
            {
                return Json(new { success = true, message = "Đã gửi nhắc nhở thành công!" });
            }
            else if (result == "Unauthorized")
            {
                return Json(new { success = false, message = "❌ Bạn không có quyền hối thúc (Chỉ Owner/Admin Team)!" });
            }
            else if (result == "SpamLimitReached")
            {
                return Json(new { success = false, message = "⚠️ Spam warning: Bạn chỉ được nhắc người này 2 lần/ngày!" });
            }
            else
            {
                return Json(new { success = false, message = "Lỗi không xác định khi gửi thông báo." });
            }
        }
    }

    // DTO để nhận dữ liệu từ fetch JSON
    public class RemoveMemberRequest
    {
        public Guid TeamId { get; set; }
        public Guid MemberId { get; set; }
    }
}
