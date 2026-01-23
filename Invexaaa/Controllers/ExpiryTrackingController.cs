using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using System;
using System.Linq;

namespace Invexaaa.Controllers
{
    public class ExpiryTrackingController : Controller
    {
        private readonly InvexaDbContext _context;

        public ExpiryTrackingController(InvexaDbContext context)
        {
            _context = context;
        }

        public IActionResult ExpiryTrackingIndex(
    string? search,
    int? categoryId,
    string? expiryStatus,
    string? expiryRange
)
        {
            var today = DateTime.Today;

            var query =
                from batch in _context.StockBatches
                join item in _context.Items on batch.ItemID equals item.ItemID
                join category in _context.Categories on item.CategoryID equals category.CategoryID
                select new ExpiryTrackingViewModel
                {
                    BatchID = batch.BatchID,
                    ItemName = item.ItemName,
                    CategoryName = category.CategoryName,
                    BatchNumber = batch.BatchNumber,
                    Quantity = batch.BatchQuantity,
                    ExpiryDate = batch.BatchExpiryDate,
                    ExpiryStatus =
                        batch.BatchExpiryDate < today ? "Expired" :
                        batch.BatchExpiryDate <= today.AddDays(30) ? "Near Expiry" :
                        "Safe"
                };

            if (!string.IsNullOrEmpty(search))
                query = query.Where(x => x.ItemName.Contains(search));

            if (categoryId.HasValue)
                query = query.Where(x =>
                    _context.Categories.Any(c =>
                        c.CategoryID == categoryId &&
                        c.CategoryName == x.CategoryName));

            if (!string.IsNullOrEmpty(expiryStatus))
                query = query.Where(x => x.ExpiryStatus == expiryStatus);

            if (expiryRange == "7")
                query = query.Where(x => x.ExpiryDate <= today.AddDays(7));
            else if (expiryRange == "30")
                query = query.Where(x => x.ExpiryDate <= today.AddDays(30));

            ViewBag.Categories = _context.Categories.ToList();

            return View(query.OrderBy(x => x.ExpiryDate).ToList());
        }

    }
}
