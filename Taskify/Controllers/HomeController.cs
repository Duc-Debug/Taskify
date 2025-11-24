using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Taskify.Models;

namespace Taskify.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Sample data for dashboard
            var dashboardViewModel = new DashboardViewModel
            {
                TotalTasks = 12,
                PendingTask = 3,
                InProgressTasks = 5,
                CompletedTasks = 4,
                UpcomingTasks = new List<TaskDetailsViewModel>
                {
                    new TaskDetailsViewModel
                    {
                        Id = 1,
                        Title = "Complete project proposal",
                        Decription = "Finish the Q4 project proposal document",
                        Priority = TaskPriority.High,
                        Status = Models.TaskStatus.InProgress,
                        CategoryName = "Work",
                        DueDate = DateTime.Now.AddDays(2),
                        CreatedAt = DateTime.Now.AddDays(-5)
                    },
                    new TaskDetailsViewModel
                    {
                        Id = 2,
                        Title = "Review team submissions",
                        Decription = "Check and provide feedback on team work",
                        Priority = TaskPriority.Medium,
                        Status = Models.TaskStatus.Pending,
                        CategoryName = "Work",
                        DueDate = DateTime.Now.AddDays(3),
                        CreatedAt = DateTime.Now.AddDays(-3)
                    },
                    new TaskDetailsViewModel
                    {
                        Id = 3,
                        Title = "Doctor appointment",
                        Decription = "Annual health checkup",
                        Priority = TaskPriority.High,
                        Status = Models.TaskStatus.Pending,
                        CategoryName = "Personal",
                        DueDate = DateTime.Now.AddDays(1),
                        CreatedAt = DateTime.Now.AddDays(-2)
                    },
                    new TaskDetailsViewModel
                    {
                        Id = 4,
                        Title = "Buy groceries",
                        Decription = "Weekly grocery shopping",
                        Priority = TaskPriority.Low,
                        Status = Models.TaskStatus.Pending,
                        CategoryName = "Shopping",
                        DueDate = DateTime.Now,
                        CreatedAt = DateTime.Now.AddDays(-1)
                    },
                    new TaskDetailsViewModel
                    {
                        Id = 5,
                        Title = "Prepare presentation",
                        Decription = "Create slides for client meeting",
                        Priority = TaskPriority.High,
                        Status = Models.TaskStatus.InProgress,
                        CategoryName = "Work",
                        DueDate = DateTime.Now.AddDays(4),
                        CreatedAt = DateTime.Now.AddDays(-4)
                    }
                }
            };

            // Set ViewBag for sidebar stats
            ViewBag.TotalTasks = dashboardViewModel.TotalTasks;
            ViewBag.HighPriorityCount = 3;
            ViewBag.MediumPriorityCount = 5;
            ViewBag.LowPriorityCount = 4;
            ViewBag.CompletedToday = 2;
            ViewBag.TotalToday = 5;

            return View(dashboardViewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
