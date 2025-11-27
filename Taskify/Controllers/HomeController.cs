using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Taskify.Models;
using Taskify.Services;

namespace Taskify.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Logic chuyển hướng: Đã đăng nhập -> Vào Dashboard
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            // 2. Logic hiển thị: Chưa đăng nhập -> Hiện Landing Page
            // Gọi Service lấy data (nếu cần hiển thị số liệu)
            var model = await _homeService.GetLandingPageDataAsync();

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}