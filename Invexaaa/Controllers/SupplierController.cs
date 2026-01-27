using Microsoft.AspNetCore.Mvc;
using Invexaaa.Data;
using Invexaaa.Models.Invexa;

namespace Invexaaa.Controllers
{
    public class SupplierController : Controller
    {
        private readonly InvexaDbContext _context;

        public SupplierController(InvexaDbContext context)
        {
            _context = context;
        }

        // =========================
        // LIST (Maintain Supplier)
        // =========================
        public IActionResult SupplierIndex()
        {
            var suppliers = _context.Suppliers.ToList();
            return View("SupplierIndex", suppliers);
        }

        // =========================
        // CREATE
        // =========================
        public IActionResult Create()
        {
            return View("CreateSupplier");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Supplier supplier, string submitAction)
        {
            if (!ModelState.IsValid)
            {
                // Validation failed → stay on same page with data
                return View("CreateSupplier", supplier);
            }

            _context.Suppliers.Add(supplier);
            _context.SaveChanges();

            // SAVE & NEW → stay on page and clear form
            if (submitAction == "saveNew")
            {
                ModelState.Clear();          // clears validation + old values
                return View("CreateSupplier", new Supplier());
            }

            // SAVE → go back to list
            return RedirectToAction(nameof(SupplierIndex));
        }


        // =========================
        // EDIT
        // =========================
        public IActionResult Edit(int id)
        {
            var supplier = _context.Suppliers.Find(id);
            if (supplier == null)
                return NotFound();

            return View("EditSupplier", supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Suppliers.Update(supplier);
                _context.SaveChanges();
                return RedirectToAction(nameof(SupplierIndex));
            }

            return View("EditSupplier", supplier);
        }

        // =========================
        // TOGGLE ACTIVE / INACTIVE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int id)
        {
            var supplier = _context.Suppliers.Find(id);
            if (supplier != null)
            {
                supplier.SupplierStatus =
                    supplier.SupplierStatus == "Active" ? "Inactive" : "Active";

                _context.SaveChanges();
            }

            return RedirectToAction(nameof(SupplierIndex));
        }

        // =========================
        // HARD DELETE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var supplier = _context.Suppliers.Find(id);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(SupplierIndex));
        }

        // =========================
        // JSON LIST (for dropdown refresh)
        // =========================
        [HttpGet]
        public IActionResult ListJson()
        {
            var suppliers = _context.Suppliers
                .Where(s => s.SupplierStatus == "Active") // optional but recommended
                .OrderBy(s => s.SupplierName)
                .Select(s => new
                {
                    id = s.SupplierID,
                    name = s.SupplierName
                })
                .ToList();

            return Json(suppliers);
        }

    }
}
