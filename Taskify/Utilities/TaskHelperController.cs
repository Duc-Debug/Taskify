using Microsoft.AspNetCore.Mvc;
using Taskify.Services;

namespace Taskify.Utilities
{
    [Route("api/[controller]")]
    [ApiController] // Dùng attribute này để làm API
    public class TaskHelperController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly IPerformanceService _performanceService;

        public TaskHelperController(ITeamService teamService, IPerformanceService performanceService)
        {
            _teamService = teamService;
            _performanceService = performanceService;
        }

        // API: Lấy gợi ý thành viên kèm điểm số
        [HttpGet("suggestions")]
        public async Task<IActionResult> GetMemberSuggestions(Guid teamId, string? title, string? desc)
        {
            // 1. Lấy tất cả thành viên team
            var members = await _teamService.GetTeamMembersWithSkillsAsync(teamId);
            var result = new List<object>();

            // 2. Tính điểm cho từng người
            foreach (var member in members)
            {
                // Nếu chưa nhập Title/Desc thì mặc định tính theo lịch sử (TaskTitle rỗng)
                int ascore = await _performanceService.CalculateSuccessProbabilityAsync(
                    member.Id, title ?? "", desc ?? "");

                result.Add(new
                {
                    id = member.Id,
                    name = member.FullName,
                    avatar = member.AvatarUrl ?? "/images/default-avatar.png",
                    score = ascore
                });
            }

            // 3. Trả về JSON (Sắp xếp điểm cao lên đầu)
            return Ok(result.OrderByDescending(x => ((dynamic)x).score));
        }
    }
}
