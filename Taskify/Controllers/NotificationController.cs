using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Taskify.Services;

namespace Taskify.Controllers
{
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ITeamService _teamService;
        public NotificationController(INotificationService notificationService, ITeamService teamService)
        {
            _notificationService = notificationService;
            _teamService = teamService;
        }
        private Guid GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(id);
        }
        [HttpPost]
        [Route("Notifications/MarkAsRead/{id}")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var result = await _notificationService.MarkAsReadAsync(id);
            if(result) return Ok(new { success = true });
            return BadRequest(new { success = false, message = "Could not mark notification as read." });
        }

        [HttpPost]
        [Route("Notifications/MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAllASReadAsync(userId);
            return Ok(new { success = true } );
        }
        [HttpPost]
        public async Task<IActionResult> RespondToApproval(Guid notificationId, bool isApproved)
        {
            // Gọi Service xử lý logic duyệt/từ chối ta vừa viết
            var result = await _teamService.HandleInviteApprovalAsync(notificationId, isApproved);

            if (result)
                return Ok(new { success = true, message = "Đã xử lý yêu cầu." });
            else
                return BadRequest("Có lỗi xảy ra.");
        }
    }
}
