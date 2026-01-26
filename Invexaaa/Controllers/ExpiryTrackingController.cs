using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Invexaaa.Data;
using Invexaaa.Models.Invexa;
using System;

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
                where item.ItemStatus == "Active"   // 🔒 HIDE INACTIVE ITEMS
                select new ExpiryTrackingViewModel
                {
                    BatchID = batch.BatchID,
                    ItemName = item.ItemName,

                    CategoryID = category.CategoryID,
                    CategoryName = category.CategoryName,

                    BatchNumber = batch.BatchNumber,
                    Quantity = batch.BatchQuantity,
                    ExpiryDate = batch.BatchExpiryDate,
                    ExpiryStatus =
                        batch.BatchExpiryDate < today ? "Expired" :
                        batch.BatchExpiryDate <= today.AddDays(30) ? "Near Expiry" :
                        "Safe"
                };

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.ItemName.Contains(search));

            if (categoryId.HasValue)
                query = query.Where(x => x.CategoryID == categoryId);

            if (!string.IsNullOrWhiteSpace(expiryStatus))
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
