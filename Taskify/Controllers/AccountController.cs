using Microsoft.AspNetCore.Mvc;
using Taskify.Models;

namespace Taskify.Controllers
{
    public class AccountController : Controller
    {
      
            // GET: Account/Login
            public IActionResult Login()
            {
                return View();
            }

            // POST: Account/Login
            [HttpPost]
            [ValidateAntiForgeryToken]
            public IActionResult Login(LoginViewModel model)
            {
                if (ModelState.IsValid)
                {
                    // Simple authentication (replace with real authentication)
                    if (model.Email == "admin@taskify.com" && model.Password == "Admin@123")
                    {
                        // Set authentication cookie or session here
                        TempData["SuccessMessage"] = "Login successful!";
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Invalid email or password.";
                        return View(model);
                    }
                }

                return View(model);
            }

            // GET: Account/Register
            public IActionResult Register()
            {
                return View();
            }

            // POST: Account/Register
            [HttpPost]
            [ValidateAntiForgeryToken]
            public IActionResult Register(RegisterViewModel model)
            {
                if (ModelState.IsValid)
                {
                    // Check if email already exists (simplified)
                    if (model.Email == "admin@taskify.com")
                    {
                        ViewBag.ErrorMessage = "Email already registered.";
                        return View(model);
                    }

                    // Register user (add to database here)
                    TempData["SuccessMessage"] = "Registration successful! Please login.";
                    return RedirectToAction(nameof(Login));
                }

                return View(model);
            }

            // GET: Account/Profile
            public IActionResult Profile()
            {
                var model = new ProfileViewModel
                {
                    FullName = "John Doe",
                    Email = "john.doe@example.com",
                    DateOfBirth = new DateTime(1990, 1, 1),
                    Address = "123 Main Street, City, Country",
                    Bio = "Task management enthusiast and productivity expert.",
                    AvatarUrl = null,
                    MemberSince = DateTime.Now.AddMonths(-6),
                    TotalTasks = 45,
                    CompletedTasks = 32,
                    PendingTasks = 13,
                    EmailNotifications = true,
                    TaskReminders = true
                };

                return View(model);
            }

            // POST: Account/UpdateProfile
            [HttpPost]
            [ValidateAntiForgeryToken]
            public IActionResult UpdateProfile(ProfileViewModel model)
            {
                if (ModelState.IsValid)
                {
                    // Update user profile in database
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction(nameof(Profile));
                }

                // Repopulate stats if validation fails
                model.MemberSince = DateTime.Now.AddMonths(-6);
                model.TotalTasks = 45;
                model.CompletedTasks = 32;
                model.PendingTasks = 13;

                return View("Profile", model);
            }

            // GET: Account/ChangePassword
            public IActionResult ChangePassword()
            {
                return View();
            }

            // POST: Account/ChangePassword
            [HttpPost]
            [ValidateAntiForgeryToken]
            public IActionResult ChangePassword(ChangePasswordViewModel model)
            {
                if (ModelState.IsValid)
                {
                    // Verify current password (simplified)
                    if (model.CurrentPassword != "Admin@123")
                    {
                        ViewBag.ErrorMessage = "Current password is incorrect.";
                        return View(model);
                    }

                    // Update password in database
                    TempData["SuccessMessage"] = "Password changed successfully!";
                    return RedirectToAction(nameof(Profile));
                }

                return View(model);
            }

            // POST: Account/Logout
            [HttpPost]
            [ValidateAntiForgeryToken]
            public IActionResult Logout()
            {
                // Clear authentication cookie or session
                TempData["SuccessMessage"] = "You have been logged out.";
                return RedirectToAction(nameof(Login));
            }

            // POST: Account/DeleteAccount
            [HttpPost]
            [ValidateAntiForgeryToken]
            public IActionResult DeleteAccount()
            {
                // Delete user account from database
                TempData["SuccessMessage"] = "Your account has been deleted.";
                return RedirectToAction(nameof(Login));
            }

            // GET: Account/ForgotPassword
            public IActionResult ForgotPassword()
            {
                return View();
            }

            // GET: Account/Settings
            public IActionResult Settings()
            {
                return RedirectToAction("Index", "Settings");
            }
        
    }
}
