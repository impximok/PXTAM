using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Invexaaa.Data;
using Invexaaa.Models.Invexa;

namespace Invexaaa.Controllers
{
    public class CategoryController : Controller
    {
        private readonly InvexaDbContext _context;

        public CategoryController(InvexaDbContext context)
        {
            _context = context;
        }

        // =========================
        // LIST
        // =========================
        public IActionResult CategoryIndex()
        {
            var categories = _context.Categories.ToList();
            return View("CategoryIndex", categories);
        }

        // =========================
        // CREATE
        // =========================
        public IActionResult Create()
        {
            return View("CreateCategory");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                _context.SaveChanges();
                return RedirectToAction(nameof(CategoryIndex));
            }

            return View("CreateCategory", category);
        }

        // =========================
        // EDIT
        // =========================
        public IActionResult Edit(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null)
                return NotFound();

            return View("EditCategory", category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Update(category);
                _context.SaveChanges();
                return RedirectToAction(nameof(CategoryIndex));
            }

            return View("EditCategory", category);
        }

        // =========================
        // TOGGLE ACTIVE / INACTIVE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int id)
        {
            var category = _context.Categories.Find(id);
            if (category != null)
            {
                category.CategoryStatus =
                    category.CategoryStatus == "Active" ? "Inactive" : "Active";

                _context.SaveChanges();
            }

            return RedirectToAction(nameof(CategoryIndex));
        }

        // =========================
        // HARD DELETE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var category = _context.Categories.Find(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(CategoryIndex));
        }

        // =========================
        // JSON LIST (for dropdown refresh)
        // =========================
        [HttpGet]
        public IActionResult ListJson()
        {
            var categories = _context.Categories
                .Where(c => c.CategoryStatus == "Active") // optional but recommended
                .OrderBy(c => c.CategoryName)
                .Select(c => new
                {
                    id = c.CategoryID,
                    name = c.CategoryName
                })
                .ToList();

            return Json(categories);
        }

    }
}
