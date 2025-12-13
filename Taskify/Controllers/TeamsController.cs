using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                // Tạo xong -> Quay về trang danh sách
                return RedirectToAction(nameof(Index));
            }

            // [QUAN TRỌNG] Nếu lỗi dữ liệu, cũng phải quay về Index (vì không có trang Create riêng)
            // Để giữ trải nghiệm tốt hơn, sau này ta sẽ dùng AJAX, còn hiện tại dùng Redirect là an toàn nhất.
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var team = await _teamService.GetTeamDetailsAsync(id, userId);

            if (team == null) return NotFound(); // Hoặc trang "Access Denied"

            return View(team);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveMember([FromBody] RemoveMemberRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _teamService.RemoveMemberAsync(request.TeamId, request.MemberId, userId);

            if (result) return Ok(new { success = true });
            return BadRequest(new { success = false, message = "Failed to remove member" });
        }
        

        // DTO để nhận dữ liệu từ fetch JSON
        public class RemoveMemberRequest
        {
            public Guid TeamId { get; set; }
            public Guid MemberId { get; set; }
        }
    }
}
