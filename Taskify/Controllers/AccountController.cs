using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Taskify.Models;
using Taskify.Services;

namespace Taskify.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        // -------- REGISTER------------
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _accountService.RegisterAsync(model.FullName, model.Email, model.Password);
                if (user == null)
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    return View(model);
                }
                await SignInUser(user, false);
                return RedirectToAction("Index", "Dashboard");
            }
            return View(model);
        }
        // ----- LOGIN-----
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _accountService.ValidateUserAsync(model.Email, model.Password);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return View(model);
                }
                await SignInUser(user, model.RememberMe);
                return RedirectToAction("Index", "Dashboard");
            }
            return View(model);
        }
        // ----- LOGOUT -----
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
        // -----Method to create Cookie Login -----
        private async Task SignInUser(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email)
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(7) : (DateTimeOffset?)null
            };
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        //---Method to Profile ----
        // -------- PROFILE (GET) ------------
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // 1. Lấy ID của user đang đăng nhập từ Cookie/Claims
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Login");
            }

            // 2. Gọi Service để lấy thông tin chi tiết (Bạn cần implement hàm này)
            // Lưu ý: Tôi giả định bạn sẽ thêm hàm GetUserProfileAsync vào IAccountService
            var userProfile = await _accountService.GetUserProfileAsync(userId);

            if (userProfile == null)
            {
                return NotFound();
            }

            return View(userProfile);
        }

        // -------- UPDATE PROFILE (POST) ------------
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Nếu dữ liệu không hợp lệ, trả về view kèm lỗi
                return View("Profile", model);
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Login");
            }

            // 3. Gọi Service để cập nhật database (Bạn cần implement hàm này)
            var result = await _accountService.UpdateProfileAsync(userId, model);

            if (result)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }
            else
            {
                ModelState.AddModelError("", "An error occurred while updating profile.");
                return View("Profile", model);
            }
        }

        // -------- CHANGE PASSWORD VIEW (GET) ------------
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }
    }
}
