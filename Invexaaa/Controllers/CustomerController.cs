using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using Microsoft.AspNetCore.Mvc;

namespace Invexaaa.Controllers
{
    public class CustomerController : Controller
    {
        private readonly InvexaDbContext _context;

        public CustomerController(InvexaDbContext context)
        {
            _context = context;
        }

        // =========================
        // INDEX
        // =========================
        public IActionResult Index()
        {
            var list = _context.Customers
                .OrderBy(c => c.CustomerName)
                .ToList();

            return View("CustomerIndex", list); // 👈 explicit view
        }

        // =========================
        // CREATE (GET)
        // =========================
        public IActionResult Create()
        {
            return View("CreateCustomer", new Customer()); // 👈 explicit view
        }

        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Customer model, string? submitAction)
        {
            if (!ModelState.IsValid)
                return View("CreateCustomer", model); // 👈 explicit view

            model.CustomerStatus = "Active";
            model.CustomerCreatedAt = DateTime.Now;

            _context.Customers.Add(model);
            _context.SaveChanges();

            TempData["Success"] = "Customer created successfully.";

            return submitAction == "saveNew"
                ? RedirectToAction(nameof(Create))
                : RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT (GET)
        // =========================
        public IActionResult Edit(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null)
                return NotFound();

            return View("EditCustomer", customer); // 👈 explicit view
        }

        // =========================
        // EDIT (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Customer model)
        {
            if (!ModelState.IsValid)
                return View("EditCustomer", model); // 👈 explicit view

            var customer = _context.Customers.Find(model.CustomerID);
            if (customer == null)
                return NotFound();

            customer.CustomerName = model.CustomerName;
            customer.CustomerPhone = model.CustomerPhone;
            customer.CustomerEmail = model.CustomerEmail;
            customer.CustomerAddress = model.CustomerAddress;
            customer.CustomerStatus = model.CustomerStatus;

            _context.SaveChanges();

            TempData["Success"] = "Customer updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // TOGGLE STATUS
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null)
                return NotFound();

            customer.CustomerStatus =
                customer.CustomerStatus == "Active" ? "Inactive" : "Active";

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null)
                return NotFound();

            bool hasTransactions = _context.StockTransactions
                .Any(t => t.CustomerID == id);

            if (hasTransactions)
            {
                TempData["Error"] = "Cannot delete — customer has stock transactions.";
                return RedirectToAction(nameof(Index));
            }

            _context.Customers.Remove(customer);
            _context.SaveChanges();

            TempData["Success"] = "Customer deleted.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // BULK DELETE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkDelete(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return RedirectToAction(nameof(Index));

            var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(int.Parse)
                            .ToList();

            bool anyUsed = _context.StockTransactions
                .Any(t => t.CustomerID != null && idList.Contains(t.CustomerID.Value));

            if (anyUsed)
            {
                TempData["Error"] = "Cannot delete — one or more customers have stock transactions.";
                return RedirectToAction(nameof(Index));
            }

            var customers = _context.Customers
                .Where(c => idList.Contains(c.CustomerID))
                .ToList();

            _context.Customers.RemoveRange(customers);
            _context.SaveChanges();

            TempData["Success"] = $"{customers.Count} customer(s) deleted.";
            return RedirectToAction(nameof(Index));
        }
    }

}
