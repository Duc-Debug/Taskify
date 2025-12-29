using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;// serialize object vào Session
using Taskify.Models;
using Taskify.Services;
using Taskify.Utilities;

namespace Taskify.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IEmailService _emailService;
        public AccountController(IAccountService accountService,IEmailService emailService)
        {
            _accountService = accountService;
            _emailService = emailService;
        }

        // -------- REGISTER------------
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _accountService.GetUserbyEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    return View(model);
                }
                var otp = new Random().Next(100000, 999999).ToString();
                HttpContext.Session.SetString("RegisterData",JsonSerializer.Serialize(model));
                HttpContext.Session.SetString("OTP", otp);
                HttpContext.Session.SetString("OTP_Expiry", DateTime.Now.AddMinutes(5).ToString());
                string subject = "Taskify - Verify your email";
                string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                    <h2 style='color: #0ea5e9;'>Verify your email address</h2>
                    <p>Thanks for starting the new Taskify account creation process. We want to make sure it's really you.</p>
                    <p>Please enter the following verification code when prompted. If you don't want to create an account, you can ignore this message.</p>
                    <h1 style='background: #f0f9ff; color: #0284c7; padding: 10px 20px; display: inline-block; border-radius: 8px;'>{otp}</h1>
                    <p>Verification code expires in 5 minutes.</p>
                </div>";
                try
                {
                    await _emailService.SendEmailAsync(model.Email, subject, body);
                    return RedirectToAction("VerifyOtp");
                }
                catch(Exception ex)
                {
                    ModelState.AddModelError("", "Could not send verification email. Please try again later. Error: " + ex.Message);
                }
            }
            return View(model);
        }

        //-======
        //  XAC THUC OTP
        //========
        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyOtp()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("RegisterData"))) return RedirectToAction("Register");

            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp(string otpCode)
        {
            var sessionOtp = HttpContext.Session.GetString("OTP");
            var expiryStr = HttpContext.Session.GetString("OTP_Expiry");
            var registerDataJson = HttpContext.Session.GetString("RegisterData");
            if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(registerDataJson))
            {
                TempData["Error"] = "Session expired. Please register again";
                return RedirectToAction("Register");
            }
            // Check mã OTP
            if (otpCode == sessionOtp)
            {
                // OTP đúng -> Tạo User thật vào Database
                var model = JsonSerializer.Deserialize<RegisterViewModel>(registerDataJson);
                if (model != null)
                {
                    // Gọi Service để tạo user (Hash password nằm trong Service)
                    var user = await _accountService.RegisterAsync(model.FullName, model.Email, model.Password);

                    if (user != null)
                    {
                        // Xóa Session
                        HttpContext.Session.Clear();

                        // Đăng nhập luôn
                        await SignInUser(user, false);
                        return RedirectToAction("Index", "Dashboard");
                    }
                }
            }

            ModelState.AddModelError("", "Invalid Verification Code.");
            return View();
        }
        // ----- LOGIN-----
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl=null)
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
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }
            return View(model);
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account");
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return RedirectToAction("Login");
            var emailClaim = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var nameClaim = result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? "Google User";
            if (string.IsNullOrEmpty(emailClaim)) 
                return RedirectToAction("Login");

            var user = await _accountService.GetUserbyEmailAsync(emailClaim);
            if(user == null)
            {
                //Chua co tk==> Tu dong dang ky
                string randomPassword = GenerateRandomPassword(10);
                user = await _accountService.RegisterAsync(nameClaim, emailClaim, randomPassword);
                if (user != null)
                {
                    string emailBody = $@"
                        <h3>Welcome to Taskify via Google!</h3>
                        <p>An account has been created for you using your Google email.</p>
                        <p>If you ever want to login with a password, use this auto-generated one:</p>
                        <h3 style='color:red;'>{randomPassword}</h3>
                        <p>You can change this password anytime in Settings.</p>";
                    await _emailService.SendEmailAsync(emailClaim, "Your Taskify Account Credentials", emailBody);
                }
            }
            if(user != null)
            {
                await SignInUser(user, false);
                return RedirectToAction("Index", "Dashboard");
            }
            return RedirectToAction("Login");
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

        // -------- CHANGE PASSWORD(FORGOT) VIEW (GET) ------------
       
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if(!ModelState.IsValid) return View(model);
            try
            {
                var userId = GetCurrentUserId();
                await _accountService.ChangePasswordASync(userId, model.CurrentPassword, model.NewPassword);
                TempData["SuccessMessage"] = "ChangePassword successffuly";
                return RedirectToAction("Profile");
            }
            catch(Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            await _accountService.SendForgotPasswordOtpAsync(model.Email);
            return RedirectToAction("ResetPassword", new { email = model.Email });
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email)
        {
            // Tạo model có sẵn Email để hiện lên View
            var model = new ResetPasswordViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            try
            {
                await _accountService.ResetPasswordWithOtpAsync(model.Email, model.OtpCode, model.NewPassword);

                // Thành công -> Về trang Login
                TempData["SuccessMessage"] = "Change password successfully, Please enter again.";
                return RedirectToAction("Login");
            }
            catch(Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }
        //-----------OTHER------------------
        private   Guid GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(id);
        }
        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
