using Microsoft.AspNetCore.Mvc;
using Taskify.Models;

namespace Taskify.Controllers
{

    public class SettingsController : Controller
    {
        // Sample user settings (in real app, load from database)
        private static SettingsViewModel _userSettings = new SettingsViewModel
        {
            EnableNotification = true,
            Theme = "Light"
        };

        // GET: Settings
        public IActionResult Index()
        {
            return View(_userSettings);
        }

        // POST: Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(SettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                _userSettings.EnableNotification = model.EnableNotification;
                _userSettings.Theme = model.Theme;

                TempData["SuccessMessage"] = "Settings saved successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Settings/Notifications
        public IActionResult Notifications()
        {
            return View(_userSettings);
        }

        // POST: Settings/Notifications
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Notifications(SettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                _userSettings.EnableNotification = model.EnableNotification;

                TempData["SuccessMessage"] = "Notification settings saved successfully!";
                return RedirectToAction(nameof(Notifications));
            }

            return View(model);
        }

        // GET: Settings/Preferences
        public IActionResult Preferences()
        {
            return View(_userSettings);
        }

        // POST: Settings/Preferences
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Preferences(SettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                _userSettings.Theme = model.Theme;

                TempData["SuccessMessage"] = "Preferences saved successfully!";
                return RedirectToAction(nameof(Preferences));
            }

            return View(model);
        }
    }
}

