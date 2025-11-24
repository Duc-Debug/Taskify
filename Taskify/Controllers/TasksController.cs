using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Taskify.Models;

namespace Taskify.Controllers
{
    public class TasksController : Controller
    {
        // Sample in-memory data store (replace with actual database)
        private static List<TaskDetailsViewModel> _tasks = new List<TaskDetailsViewModel>
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
            }
        };

        // GET: Tasks
        public IActionResult Index(string filter = null, string priority = null, string status = null)
        {
            var tasks = _tasks.AsEnumerable();

            // Apply filters
            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<TaskPriority>(priority, out var priorityEnum))
            {
                tasks = tasks.Where(t => t.Priority == priorityEnum);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<Models.TaskStatus>(status, out var statusEnum))
            {
                tasks = tasks.Where(t => t.Status == statusEnum);
            }

            if (filter == "today")
            {
                tasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.Today);
            }
            else if (filter == "week")
            {
                var endOfWeek = DateTime.Today.AddDays(7);
                tasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date <= endOfWeek);
            }
            else if (filter == "overdue")
            {
                tasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.Now && t.Status != Models.TaskStatus.Completed);
            }
            else if (filter == "completed")
            {
                tasks = tasks.Where(t => t.Status == Models.TaskStatus.Completed);
            }

            // Set ViewBag stats
            ViewBag.TotalTasks = _tasks.Count;
            ViewBag.CompletedTasks = _tasks.Count(t => t.Status == Models.TaskStatus.Completed);
            ViewBag.InProgressTasks = _tasks.Count(t => t.Status == Models.TaskStatus.InProgress);
            ViewBag.OverdueTasks = _tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.Now && t.Status != Models.TaskStatus.Completed);

            return View(tasks.ToList());
        }

        // GET: Tasks/Details/5
        public IActionResult Details(int id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // GET: Tasks/Create
        public IActionResult Create()
        {
            var model = new TaskCreateViewModel
            {
                Categories = GetCategoriesSelectList()
            };
            return View(model);
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(TaskCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var newTask = new TaskDetailsViewModel
                {
                    Id = _tasks.Any() ? _tasks.Max(t => t.Id) + 1 : 1,
                    Title = model.Title,
                    Decription = model.Description,
                    Priority = model.Priority,
                    Status = Models.TaskStatus.Pending,
                    CategoryName = GetCategoryName(model.CategoryId),
                    DueDate = model.DueDate,
                    CreatedAt = DateTime.Now
                };

                _tasks.Add(newTask);
                TempData["SuccessMessage"] = "Task created successfully!";
                return RedirectToAction(nameof(Index));
            }

            model.Categories = GetCategoriesSelectList();
            return View(model);
        }

        // GET: Tasks/Edit/5
        public IActionResult Edit(int id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            var model = new TaskEditViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Decription,
                Priority = task.Priority,
                Status = task.Status,
                DueDate = task.DueDate,
                Categories = GetCategoriesSelectList(),
                CategoryId = 1 // Default value
            };

            return View(model);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(TaskEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == model.Id);
                if (task == null)
                {
                    return NotFound();
                }

                task.Title = model.Title;
                task.Decription = model.Description;
                task.Priority = model.Priority;
                task.Status = model.Status;
                task.DueDate = model.DueDate;
                task.CategoryName = GetCategoryName(model.CategoryId);

                if (model.Status == Models.TaskStatus.Completed && !task.CompletedAt.HasValue)
                {
                    task.CompletedAt = DateTime.Now;
                }

                TempData["SuccessMessage"] = "Task updated successfully!";
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }

            model.Categories = GetCategoriesSelectList();
            return View(model);
        }

        // POST: Tasks/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            _tasks.Remove(task);
            TempData["SuccessMessage"] = "Task deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Tasks/UpdateStatus/5
        [HttpPost]
        public IActionResult UpdateStatus(int id, [FromBody] StatusUpdateModel model)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            if (Enum.TryParse<Models.TaskStatus>(model.Status, out var status))
            {
                task.Status = status;
                if (status == Models.TaskStatus.Completed)
                {
                    task.CompletedAt = DateTime.Now;
                }
                else
                {
                    task.CompletedAt = null;
                }
            }

            return Ok();
        }

        // POST: Tasks/QuickCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuickCreate(TaskCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var newTask = new TaskDetailsViewModel
                {
                    Id = _tasks.Any() ? _tasks.Max(t => t.Id) + 1 : 1,
                    Title = model.Title,
                    Decription = model.Description,
                    Priority = model.Priority,
                    Status = Models.TaskStatus.Pending,
                    CategoryName = GetCategoryName(model.CategoryId),
                    DueDate = model.DueDate,
                    CreatedAt = DateTime.Now
                };

                _tasks.Add(newTask);
                TempData["SuccessMessage"] = "Task created successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper methods
        private IEnumerable<SelectListItem> GetCategoriesSelectList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Work" },
                new SelectListItem { Value = "2", Text = "Personal" },
                new SelectListItem { Value = "3", Text = "Shopping" },
                new SelectListItem { Value = "4", Text = "Health" },
                new SelectListItem { Value = "5", Text = "Other" }
            };
        }

        private string GetCategoryName(int categoryId)
        {
            return categoryId switch
            {
                1 => "Work",
                2 => "Personal",
                3 => "Shopping",
                4 => "Health",
                5 => "Other",
                _ => "Uncategorized"
            };
        }
    }

    // Helper model for status update
    public class StatusUpdateModel
    {
        public string Status { get; set; }
    }
}
