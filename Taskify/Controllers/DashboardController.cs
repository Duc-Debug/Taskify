using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Taskify.Services;

namespace Taskify.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }
       public async Task<IActionResult> Index()
        {
            //Lấy userId từ Claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString,out var userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var dashboardModel = await _dashboardService.GetDashboardDataAsync(userId);
            return View(dashboardModel);
        }
    }
}
