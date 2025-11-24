using Microsoft.AspNetCore.Mvc;
using Taskify.Models;

namespace Taskify.Controllers
{
    public class CategoriesController : Controller
    {
        // Sample in-memory data store
        private static List<CategoryViewModel> _categories = new List<CategoryViewModel>
        {
            new CategoryViewModel
            {
                Id = 1,
                Name = "Work",
                Description = "Work-related tasks and projects"
            },
            new CategoryViewModel
            {
                Id = 2,
                Name = "Personal",
                Description = "Personal errands and activities"
            },
            new CategoryViewModel
            {
                Id = 3,
                Name = "Shopping",
                Description = "Shopping lists and purchases"
            },
            new CategoryViewModel
            {
                Id = 4,
                Name = "Health",
                Description = "Health and fitness related tasks"
            },
            new CategoryViewModel
            {
                Id = 5,
                Name = "Other",
                Description = "Miscellaneous tasks"
            }
        };

        // GET: Categories
        public IActionResult Index()
        {
            return View(_categories);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate names
                if (_categories.Any(c => c.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("Name", "A category with this name already exists.");
                    return View(model);
                }

                model.Id = _categories.Any() ? _categories.Max(c => c.Id) + 1 : 1;
                _categories.Add(model);

                TempData["SuccessMessage"] = "Category created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Categories/Edit/5
        public IActionResult Edit(int id)
        {
            var category = _categories.FirstOrDefault(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            // Sample task counts
            ViewBag.TotalTasks = new Random().Next(0, 20);
            ViewBag.CompletedTasks = new Random().Next(0, 10);
            ViewBag.PendingTasks = ViewBag.TotalTasks - ViewBag.CompletedTasks;

            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var category = _categories.FirstOrDefault(c => c.Id == model.Id);
                if (category == null)
                {
                    return NotFound();
                }

                // Check for duplicate names (excluding current category)
                if (_categories.Any(c => c.Id != model.Id && c.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("Name", "A category with this name already exists.");

                    ViewBag.TotalTasks = new Random().Next(0, 20);
                    ViewBag.CompletedTasks = new Random().Next(0, 10);
                    ViewBag.PendingTasks = ViewBag.TotalTasks - ViewBag.CompletedTasks;

                    return View(model);
                }

                category.Name = model.Name;
                category.Description = model.Description;

                TempData["SuccessMessage"] = "Category updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.TotalTasks = new Random().Next(0, 20);
            ViewBag.CompletedTasks = new Random().Next(0, 10);
            ViewBag.PendingTasks = ViewBag.TotalTasks - ViewBag.CompletedTasks;

            return View(model);
        }

        // GET: Categories/Delete/5
        public IActionResult Delete(int id)
        {
            var category = _categories.FirstOrDefault(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            // Sample task counts
            ViewBag.TotalTasks = new Random().Next(0, 20);
            ViewBag.CompletedTasks = new Random().Next(0, 10);
            ViewBag.PendingTasks = ViewBag.TotalTasks - ViewBag.CompletedTasks;

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(CategoryViewModel model)
        {
            var category = _categories.FirstOrDefault(c => c.Id == model.Id);
            if (category == null)
            {
                return NotFound();
            }

            _categories.Remove(category);
            TempData["SuccessMessage"] = "Category deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
