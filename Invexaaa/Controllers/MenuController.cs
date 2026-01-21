using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SnomiAssignmentReal.Data;
using SnomiAssignmentReal.Helpers;
using SnomiAssignmentReal.Models;
using SnomiAssignmentReal.Models.ViewModels;
using Microsoft.AspNetCore.Http;      // IFormFile
using Microsoft.AspNetCore.Http.Features;
using System.IO; // for Path, Directory, File


namespace SnomiAssignmentReal.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public MenuController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ------------------- CUSTOMER -------------------
        [AllowAnonymous]
        public async Task<IActionResult> Catalog(
    string? q,
    string? categoryId,
    bool? onlyAvailable,
    string sort = "name_asc",
    int page = 1,
    int pageSize = 12
)
        {
            page = Math.Max(1, page);
            pageSize = (pageSize < 6 || pageSize > 60) ? 12 : pageSize;

            var query = _db.MenuItems
                .Include(m => m.Category)
                .AsNoTracking()
                .AsQueryable();

            // filters
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(m => m.MenuItemName.Contains(q) || m.MenuItemDescription!.Contains(q));

            if (!string.IsNullOrWhiteSpace(categoryId))
                query = query.Where(m => m.CategoryId == categoryId);

            if (onlyAvailable == true)
                query = query.Where(m => m.IsAvailableForOrder);

            // total BEFORE paging
            var total = await query.CountAsync();

            // primary order by Category to keep groups intact
            IOrderedQueryable<MenuItem> ordered = query.OrderBy(m => m.Category!.CategoryName);

            // secondary sort within each category
            ordered = sort switch
            {
                "price_asc" => ordered.ThenBy(m => m.MenuItemUnitPrice).ThenBy(m => m.MenuItemName),
                "price_desc" => ordered.ThenByDescending(m => m.MenuItemUnitPrice).ThenBy(m => m.MenuItemName),
                "cal_asc" => ordered.ThenBy(m => m.MenuItemCalories).ThenBy(m => m.MenuItemName),
                "cal_desc" => ordered.ThenByDescending(m => m.MenuItemCalories).ThenBy(m => m.MenuItemName),
                "name_desc" => ordered.ThenByDescending(m => m.MenuItemName),
                _ => ordered.ThenBy(m => m.MenuItemName) // name_asc
            };

            // paging (even for grouped view)
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;
            var effectivePage = Math.Max(page, 1);

            var items = await ordered
                .Skip((effectivePage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // category chips (global counts)
            var chipData = await _db.Categories
                .Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName,
                    Count = c.CategoryMenuItems.Count()
                })
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            ViewBag.CategoryChips = chipData;

            // pass state
            ViewBag.Page = effectivePage;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = totalPages;

            ViewBag.FilterQ = q ?? "";
            ViewBag.FilterCategoryId = categoryId ?? "";
            ViewBag.FilterOnlyAvailable = onlyAvailable ?? false;
            ViewBag.Sort = sort;

            // we always render the grouped (categorized) view now
            ViewBag.ViewMode = "grouped";

            return View(items);
        }




        [AllowAnonymous]
        public async Task<IActionResult> Details(string id)
        {
            var item = await _db.MenuItems
                .Include(m => m.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MenuItemId == id);

            if (item == null) return NotFound();

            var customizations = await _db.OrderCustomizationSettings
                .Where(c => c.MenuItemId == id)
                .OrderBy(c => c.CustomizationAdditionalPrice).ThenBy(c => c.CustomizationName)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Customizations = customizations;
            return View(item);
        }

        // ------------------- ADMIN: ITEMS -------------------
        public IActionResult Index(
            string? q,
            string? categoryId,
            bool? onlyAvailable,
            int page = 1,
            int pageSize = 12)
        {
            page = Math.Max(1, page);
            pageSize = (pageSize < 6 || pageSize > 60) ? 12 : pageSize;

            var query = _db.MenuItems.Include(m => m.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(m => m.MenuItemName.Contains(q) || m.MenuItemDescription.Contains(q));

            if (!string.IsNullOrWhiteSpace(categoryId))
                query = query.Where(m => m.CategoryId == categoryId);

            if (onlyAvailable == true)
                query = query.Where(m => m.IsAvailableForOrder);

            // totals BEFORE paging
            var total = query.Count();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // page slice
            var items = query
                .OrderBy(m => m.Category.CategoryName).ThenBy(m => m.MenuItemName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToList();

            // customization counts only for items on this page
            var ids = items.Select(i => i.MenuItemId).ToList();
            var counts = _db.OrderCustomizationSettings
                .Where(c => ids.Contains(c.MenuItemId))
                .GroupBy(c => c.MenuItemId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToList()
                .ToDictionary(x => x.Key, x => x.Count);

            // pass filters + paging to the view
            ViewBag.CustomCounts = counts;
            ViewBag.FilterQ = q ?? "";
            ViewBag.FilterCategoryId = categoryId ?? "";
            ViewBag.FilterOnlyAvailable = onlyAvailable ?? false;

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = totalPages;

            var categories = _db.Categories
                .OrderBy(c => c.CategoryName)
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId,
                    Text = $"{c.CategoryId} — {c.CategoryName}",
                    Selected = c.CategoryId == categoryId
                })
                .ToList();

            categories.Insert(0, new SelectListItem { Value = "", Text = "All categories", Selected = string.IsNullOrEmpty(categoryId) });

            ViewBag.Categories = categories;

            return View(items);
        }

        public IActionResult Create()
        {
            ViewBag.Categories = BuildCategorySelectList(includePrompt: true);
            ViewBag.CategoriesForEligible = BuildCategorySelectList(includePrompt: true);

            var lib = _db.OrderCustomizationSettings
                .AsEnumerable()
                .GroupBy(c => new { c.CustomizationName, c.CustomizationAdditionalPrice, c.CustomizationDescription, c.EligibleCategoryId })
                .Select(g => g.First())
                .OrderBy(c => c.EligibleCategoryId).ThenBy(c => c.CustomizationName).ThenBy(c => c.CustomizationAdditionalPrice)
                .ToList();

            ViewBag.Library = lib;
            return View(new CreateMenuItemViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateMenuItemViewModel vm)
        {
            ViewBag.Categories = BuildCategorySelectList(includePrompt: true);
            ViewBag.CategoriesForEligible = BuildCategorySelectList(includePrompt: true);

            var lib = _db.OrderCustomizationSettings
                .AsEnumerable()
                .GroupBy(c => new { c.CustomizationName, c.CustomizationAdditionalPrice, c.CustomizationDescription, c.EligibleCategoryId })
                .Select(g => g.First())
                .OrderBy(c => c.EligibleCategoryId).ThenBy(c => c.CustomizationName).ThenBy(c => c.CustomizationAdditionalPrice)
                .ToList();
            ViewBag.Library = lib;

            if (!_db.Categories.Any(c => c.CategoryId == vm.CategoryId))
                ModelState.AddModelError(nameof(vm.CategoryId), "Please choose a valid category.");

            var names = Request.Form["cust_name"];
            var prices = Request.Form["cust_price"];
            var descs = Request.Form["cust_desc"];
            var cats = Request.Form["cust_cat"];

            if (names.Count + prices.Count + descs.Count + cats.Count > 0)
            {
                int max = Math.Max(Math.Max(names.Count, prices.Count), Math.Max(descs.Count, cats.Count));
                for (int i = 0; i < max; i++)
                {
                    var name = (i < names.Count) ? names[i]?.Trim() : null;
                    var cat = (i < cats.Count) ? cats[i]?.Trim() : null;

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        if (string.IsNullOrWhiteSpace(cat) || !_db.Categories.Any(c => c.CategoryId == cat))
                        {
                            ModelState.AddModelError("MenuItemCustomizations", $"Customization “{name}” must have a valid category ID.");
                            break;
                        }
                    }
                }
            }

            if (!ModelState.IsValid) return View(vm);

            string imagePath = "/images/default.png";

            // Prefer cropped/captured data URL if provided
            var captured = Request.Form["CapturedImageDataUrl"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(captured))
            {
                imagePath = SaveDataUrlToMenu(captured);
            }
            else if (vm.ImageFile != null)
            {
                var error = FileHelper.ValidateImage(vm.ImageFile);
                if (!string.IsNullOrEmpty(error))
                {
                    ModelState.AddModelError(nameof(vm.ImageFile), error);
                    return View(vm);
                }
                var fileName = FileHelper.SaveFile(vm.ImageFile, "images/menu", _env.WebRootPath);
                imagePath = $"/images/menu/{fileName}";
            }


            var newMenuId = GenerateMenuItemId_M3();
            var item = new MenuItem
            {
                MenuItemId = newMenuId,
                MenuItemName = vm.Name?.Trim(),
                MenuItemDescription = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
                MenuItemCalories = vm.Calories,
                CategoryId = vm.CategoryId,
                MenuItemUnitPrice = vm.Price,
                IsAvailableForOrder = vm.IsAvailable,
                MenuItemImageUrl = imagePath
            };
            _db.MenuItems.Add(item);
            _db.SaveChanges();

            if (names.Count + prices.Count + descs.Count + cats.Count > 0)
            {
                int ocSeq = GetNextOcNumber();
                int max = Math.Max(Math.Max(names.Count, prices.Count), Math.Max(descs.Count, cats.Count));
                for (int i = 0; i < max; i++)
                {
                    var name = (i < names.Count) ? names[i]?.Trim() : null;
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    decimal price = 0m;
                    if (!decimal.TryParse((i < prices.Count) ? prices[i] : null, NumberStyles.Any, CultureInfo.InvariantCulture, out price))
                        price = 0m;

                    var desc = (i < descs.Count) ? descs[i]?.Trim() : null;
                    var cat = (i < cats.Count) ? cats[i]?.Trim() : null;

                    if (string.IsNullOrWhiteSpace(cat) || !_db.Categories.Any(c => c.CategoryId == cat))
                        continue;

                    _db.OrderCustomizationSettings.Add(new OrderCustomizationSettings
                    {
                        MenuItemCustomizationId = FormatOcId(ocSeq++, item.MenuItemId),
                        MenuItemId = item.MenuItemId,
                        CustomizationName = name!,
                        CustomizationAdditionalPrice = price,
                        CustomizationDescription = string.IsNullOrWhiteSpace(desc) ? null : desc,
                        EligibleCategoryId = cat
                    });
                }
                _db.SaveChanges();
            }

            TempData["Info"] = "Menu item created.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(string id)
        {
            var item = _db.MenuItems
                .Include(m => m.Category)
                .FirstOrDefault(x => x.MenuItemId == id);
            if (item == null) return NotFound();

            ViewBag.Categories = BuildCategorySelectList(selected: item.CategoryId, includePrompt: true);
            ViewBag.CategoriesForEligible = BuildCategorySelectList(includePrompt: true);

            var customs = _db.OrderCustomizationSettings
                .Where(c => c.MenuItemId == id)
                .OrderBy(c => c.CustomizationAdditionalPrice).ThenBy(c => c.CustomizationName)
                .ToList();
            ViewBag.Customizations = customs;

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 20_000_000)]
        public IActionResult Edit(MenuItem item, [FromForm(Name = "ImageFile")] IFormFile? imageFile)
        {
            ViewBag.Categories = BuildCategorySelectList(selected: item.CategoryId, includePrompt: true);
            ViewBag.CategoriesForEligible = BuildCategorySelectList(includePrompt: true);



            // 🧽 Ignore validation for properties we don't post directly
            ModelState.Remove(nameof(MenuItem.MenuItemImageUrl)); // we'll set it ourselves
            ModelState.Remove(nameof(MenuItem.Category)); // navigation property, not posted

            // Your existing validations
            if (!_db.Categories.Any(c => c.CategoryId == item.CategoryId))
                ModelState.AddModelError(nameof(item.CategoryId), "Please choose a valid category.");
            if (item.MenuItemUnitPrice < 0 || item.MenuItemUnitPrice > 1000)
                ModelState.AddModelError(nameof(item.MenuItemUnitPrice), "MenuItemUnitPrice must be between 0 and 1000.");
            if (item.MenuItemCalories < 0 || item.MenuItemCalories > 4000)
                ModelState.AddModelError(nameof(item.MenuItemCalories), "MenuItemCalories must be between 0 and 4000.");
            if (string.IsNullOrWhiteSpace(item.MenuItemName))
                ModelState.AddModelError(nameof(item.MenuItemName), "Menu item name is required.");

            var existing = _db.MenuItems.FirstOrDefault(m => m.MenuItemId == item.MenuItemId);
            if (existing == null) return NotFound();

            // Validate the file (optional)
            if (imageFile != null && imageFile.Length > 0)
            {
                var error = FileHelper.ValidateImage(imageFile);
                if (!string.IsNullOrEmpty(error))
                    ModelState.AddModelError(string.Empty, error); // put in summary
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Customizations = _db.OrderCustomizationSettings
                    .Where(c => c.MenuItemId == item.MenuItemId)
                    .OrderBy(c => c.CustomizationAdditionalPrice).ThenBy(c => c.CustomizationName)
                    .ToList();
                return View(item);
            }

            // Update scalar fields
            existing.MenuItemName = item.MenuItemName.Trim();
            existing.MenuItemDescription = string.IsNullOrWhiteSpace(item.MenuItemDescription) ? null : item.MenuItemDescription.Trim();
            existing.MenuItemUnitPrice = item.MenuItemUnitPrice;
            existing.MenuItemCalories = item.MenuItemCalories;
            existing.CategoryId = item.CategoryId;
            // Do NOT set IsAvailableForOrder here; you toggle it elsewhere.

            // Image handling
            var captured = Request.Form["CapturedImageDataUrl"].FirstOrDefault();
            bool replaceWithCaptured = !string.IsNullOrWhiteSpace(captured);
            bool replaceWithUpload = imageFile != null && imageFile.Length > 0;

            if (replaceWithCaptured || replaceWithUpload)
            {
                var oldUrl = existing.MenuItemImageUrl;

                var webRoot = _env.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRoot))
                    webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

                string newUrl;
                if (replaceWithCaptured)
                {
                    newUrl = SaveDataUrlToMenu(captured!);
                }
                else
                {
                    var error = FileHelper.ValidateImage(imageFile!);
                    if (!string.IsNullOrEmpty(error))
                    {
                        ModelState.AddModelError(string.Empty, error);
                        ViewBag.Customizations = _db.OrderCustomizationSettings
                            .Where(c => c.MenuItemId == item.MenuItemId)
                            .OrderBy(c => c.CustomizationAdditionalPrice).ThenBy(c => c.CustomizationName)
                            .ToList();
                        return View(item);
                    }
                    Directory.CreateDirectory(Path.Combine(webRoot, "images", "menu"));
                    var newFileName = FileHelper.SaveFile(imageFile!, "images/menu", webRoot);
                    newUrl = $"/images/menu/{newFileName}";
                }

                existing.MenuItemImageUrl = newUrl;

                if (!string.IsNullOrWhiteSpace(oldUrl)
                    && oldUrl.StartsWith("/images/menu", StringComparison.OrdinalIgnoreCase)
                    && !oldUrl.EndsWith("/default.png", StringComparison.OrdinalIgnoreCase))
                {
                    FileHelper.DeleteFile(oldUrl, webRoot);
                }

                TempData["Info"] = "Menu item updated and image replaced.";
            }
            else
            {
                TempData["Info"] = "Menu item updated.";
            }


            _db.SaveChanges();
            return RedirectToAction(nameof(Edit), new { id = item.MenuItemId });
        }



        public IActionResult Delete(string id)
        {
            var item = _db.MenuItems.Include(m => m.Category).FirstOrDefault(x => x.MenuItemId == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id)
        {
            var item = _db.MenuItems.FirstOrDefault(x => x.MenuItemId == id);
            if (item != null)
            {
                _db.MenuItems.Remove(item);
                _db.SaveChanges();
                TempData["Info"] = "Menu item deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ------------------- Quick toggle -------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleAvailability(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var stub = new MenuItem { MenuItemId = id };
            _db.Attach(stub);

            var current = _db.MenuItems.AsNoTracking().FirstOrDefault(m => m.MenuItemId == id);
            if (current == null) return NotFound();

            stub.IsAvailableForOrder = !current.IsAvailableForOrder;
            _db.Entry(stub).Property(x => x.IsAvailableForOrder).IsModified = true;

            _db.SaveChanges();

            TempData["Info"] = $"Availability set to {(stub.IsAvailableForOrder ? "Available" : "Unavailable")}.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // ------------------- ADMIN: CUSTOMIZATIONS -------------------
        public IActionResult Customizations(string menuItemId)
        {
            var item = _db.MenuItems.Include(m => m.Category).FirstOrDefault(m => m.MenuItemId == menuItemId);
            if (item == null) return NotFound();

            var list = _db.OrderCustomizationSettings
                .Where(c => c.MenuItemId == menuItemId)
                .OrderBy(c => c.CustomizationAdditionalPrice).ThenBy(c => c.CustomizationName)
                .ToList();

            ViewBag.MenuItem = item;
            return View(list);
        }

        public IActionResult AddFromLibrary(string menuItemId)
        {
            var item = _db.MenuItems.Include(m => m.Category).FirstOrDefault(m => m.MenuItemId == menuItemId);
            if (item == null) return NotFound();

            var library = _db.OrderCustomizationSettings
                .Where(c => c.MenuItemId != menuItemId && c.EligibleCategoryId == item.CategoryId)
                .AsEnumerable()
                .GroupBy(c => new { c.CustomizationName, c.CustomizationAdditionalPrice, c.CustomizationDescription, c.EligibleCategoryId })
                .Select(g => g.First())
                .OrderBy(c => c.EligibleCategoryId).ThenBy(c => c.CustomizationName).ThenBy(c => c.CustomizationAdditionalPrice)
                .ToList();

            var existingKeys = _db.OrderCustomizationSettings
                .Where(c => c.MenuItemId == menuItemId)
                .Select(c => new { c.CustomizationName, c.CustomizationAdditionalPrice, c.EligibleCategoryId })
                .ToList()
                .ToHashSet();

            var filtered = library.Where(c => !existingKeys.Contains(new { c.CustomizationName, c.CustomizationAdditionalPrice, c.EligibleCategoryId })).ToList();

            ViewBag.MenuItem = item;
            return View(filtered);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddFromLibrary(string menuItemId, string[] pickIds)
        {
            var item = _db.MenuItems.FirstOrDefault(m => m.MenuItemId == menuItemId);
            if (item == null) return NotFound();

            if (pickIds == null || pickIds.Length == 0)
            {
                TempData["Info"] = "No customizations selected.";
                return RedirectToAction(nameof(Customizations), new { menuItemId });
            }

            var chosen = _db.OrderCustomizationSettings
                .Where(c => pickIds.Contains(c.MenuItemCustomizationId))
                .Select(c => new { c.CustomizationName, c.CustomizationAdditionalPrice, c.CustomizationDescription, c.EligibleCategoryId })
                .ToList();

            var existing = _db.OrderCustomizationSettings
                .Where(c => c.MenuItemId == menuItemId)
                .Select(c => new { c.CustomizationName, c.CustomizationAdditionalPrice, c.EligibleCategoryId })
                .ToList()
                .ToHashSet();

            int added = 0, skipped = 0;
            int ocSeq = GetNextOcNumber();

            foreach (var proto in chosen)
            {
                if (!string.Equals(proto.EligibleCategoryId, item.CategoryId, StringComparison.OrdinalIgnoreCase))
                {
                    skipped++;
                    continue;
                }

                var key = new { proto.CustomizationName, proto.CustomizationAdditionalPrice, proto.EligibleCategoryId };
                if (existing.Contains(key)) { skipped++; continue; }

                var c = new OrderCustomizationSettings
                {
                    MenuItemCustomizationId = FormatOcId(ocSeq++, menuItemId),
                    MenuItemId = menuItemId,
                    CustomizationName = proto.CustomizationName,
                    CustomizationAdditionalPrice = proto.CustomizationAdditionalPrice,
                    CustomizationDescription = proto.CustomizationDescription,
                    EligibleCategoryId = proto.EligibleCategoryId
                };
                _db.OrderCustomizationSettings.Add(c);
                existing.Add(key);
                added++;
            }

            if (added > 0) _db.SaveChanges();
            TempData["Info"] = $"Added {added} customization(s). {(skipped > 0 ? $"Skipped {skipped} not matching/duplicate(s)." : "")}";
            return RedirectToAction(nameof(Customizations), new { menuItemId });
        }

        // ---------- Create Customization ----------
        public IActionResult CreateCustomization(string menuItemId)
        {
            var item = _db.MenuItems.Include(m => m.Category).FirstOrDefault(m => m.MenuItemId == menuItemId);
            if (item == null) return NotFound();

            ViewBag.MenuItem = item;
            ViewBag.CategoriesForEligible = BuildCategorySelectList(includePrompt: true);

            return View(new OrderCustomizationSettings
            {
                MenuItemId = menuItemId,
                CustomizationAdditionalPrice = 0m
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCustomization(OrderCustomizationSettings vm)
        {
            ModelState.Remove(nameof(OrderCustomizationSettings.MenuItemCustomizationId));
            ModelState.Remove("MenuItem");

            if (string.IsNullOrWhiteSpace(vm.MenuItemId) || !_db.MenuItems.Any(m => m.MenuItemId == vm.MenuItemId))
            {
                TempData["Error"] = "Menu item is missing or invalid.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(vm.EligibleCategoryId) ||
                !_db.Categories.Any(c => c.CategoryId == vm.EligibleCategoryId))
            {
                ModelState.AddModelError(nameof(vm.EligibleCategoryId), "Please choose a valid category.");
            }

            if (string.IsNullOrWhiteSpace(vm.CustomizationName))
                ModelState.AddModelError(nameof(vm.CustomizationName), "CategoryName is required.");
            if (vm.CustomizationAdditionalPrice < 0)
                ModelState.AddModelError(nameof(vm.CustomizationAdditionalPrice), "MenuItemUnitPrice cannot be negative.");

            if (!ModelState.IsValid)
            {
                ViewBag.MenuItem = _db.MenuItems.FirstOrDefault(m => m.MenuItemId == vm.MenuItemId);
                ViewBag.CategoriesForEligible = BuildCategorySelectList(selected: vm.EligibleCategoryId, includePrompt: true);
                return View(vm);
            }

            vm.MenuItemCustomizationId = FormatOcId(GetNextOcNumber(), vm.MenuItemId);

            try
            {
                _db.OrderCustomizationSettings.Add(vm);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to save customization: " + (ex.InnerException?.Message ?? ex.Message);
                ViewBag.MenuItem = _db.MenuItems.FirstOrDefault(m => m.MenuItemId == vm.MenuItemId);
                ViewBag.CategoriesForEligible = BuildCategorySelectList(selected: vm.EligibleCategoryId, includePrompt: true);
                return View(vm);
            }

            TempData["Info"] = "Customization created.";
            return RedirectToAction(nameof(Customizations), new { menuItemId = vm.MenuItemId });
        }

        // ---------- Edit Customization ----------
        public IActionResult EditCustomization(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound("Customization ID is required");

            var c = _db.OrderCustomizationSettings.FirstOrDefault(x => x.MenuItemCustomizationId == id);
            if (c == null)
            {
                TempData["Error"] = "Customization not found";
                return NotFound();
            }

            var menuItem = _db.MenuItems.Include(m => m.Category).FirstOrDefault(m => m.MenuItemId == c.MenuItemId);
            if (menuItem == null)
            {
                TempData["Error"] = "Associated menu item not found";
                return NotFound();
            }

            ViewBag.MenuItem = menuItem;
            ViewBag.CategoriesForEligible = BuildCategorySelectList(selected: c.EligibleCategoryId, includePrompt: true);

            return View(c);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCustomization(OrderCustomizationSettings vm)
        {
            ModelState.Remove(nameof(OrderCustomizationSettings.MenuItemCustomizationId));
            ModelState.Remove("MenuItem");

            if (string.IsNullOrWhiteSpace(vm.MenuItemCustomizationId) || string.IsNullOrWhiteSpace(vm.MenuItemId))
            {
                TempData["Error"] = "Invalid customization/menu item ID.";
                return RedirectToAction(nameof(Index));
            }

            var existing = _db.OrderCustomizationSettings.FirstOrDefault(x => x.MenuItemCustomizationId == vm.MenuItemCustomizationId);
            if (existing == null)
            {
                TempData["Error"] = "Customization not found";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(vm.EligibleCategoryId) ||
                !_db.Categories.Any(c => c.CategoryId == vm.EligibleCategoryId))
            {
                ModelState.AddModelError(nameof(vm.EligibleCategoryId), "Please choose a valid category.");
            }

            if (string.IsNullOrWhiteSpace(vm.CustomizationName))
                ModelState.AddModelError(nameof(vm.CustomizationName), "CategoryName is required");
            if (vm.CustomizationAdditionalPrice < 0)
                ModelState.AddModelError(nameof(vm.CustomizationAdditionalPrice), "MenuItemUnitPrice cannot be negative");

            if (!ModelState.IsValid)
            {
                ViewBag.MenuItem = _db.MenuItems.Include(m => m.Category).FirstOrDefault(m => m.MenuItemId == vm.MenuItemId);
                ViewBag.CategoriesForEligible = BuildCategorySelectList(selected: vm.EligibleCategoryId, includePrompt: true);
                return View(vm);
            }

            existing.CustomizationName = vm.CustomizationName.Trim();
            existing.CustomizationDescription = string.IsNullOrWhiteSpace(vm.CustomizationDescription) ? null : vm.CustomizationDescription.Trim();
            existing.CustomizationAdditionalPrice = vm.CustomizationAdditionalPrice;
            existing.EligibleCategoryId = vm.EligibleCategoryId;

            try
            {
                _db.OrderCustomizationSettings.Update(existing);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to update customization: " + (ex.InnerException?.Message ?? ex.Message);
                ViewBag.MenuItem = _db.MenuItems.Include(m => m.Category).FirstOrDefault(m => m.MenuItemId == vm.MenuItemId);
                ViewBag.CategoriesForEligible = BuildCategorySelectList(selected: vm.EligibleCategoryId, includePrompt: true);
                return View(vm);
            }

            TempData["Info"] = "Customization updated.";
            return RedirectToAction(nameof(Customizations), new { menuItemId = vm.MenuItemId });
        }

        public IActionResult DeleteCustomization(string id)
        {
            var c = _db.OrderCustomizationSettings.FirstOrDefault(x => x.MenuItemCustomizationId == id);
            if (c == null) return NotFound();

            ViewBag.MenuItem = _db.MenuItems.FirstOrDefault(m => m.MenuItemId == c.MenuItemId);
            return View(c);
        }

        [HttpPost, ActionName("DeleteCustomization")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCustomizationConfirmed(string id)
        {
            var c = _db.OrderCustomizationSettings.FirstOrDefault(x => x.MenuItemCustomizationId == id);
            if (c == null) return NotFound();

            var menuItemId = c.MenuItemId;
            _db.OrderCustomizationSettings.Remove(c);
            _db.SaveChanges();
            TempData["Info"] = "Customization deleted.";
            return RedirectToAction(nameof(Customizations), new { menuItemId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuickAddCustomization(string menuItemId, string name, decimal price, string? description, string? eligibleCategoryId)
        {
            if (string.IsNullOrWhiteSpace(menuItemId) || string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "CategoryName and Menu Item are required.";
                return RedirectToAction(nameof(Edit), new { id = menuItemId });
            }

            if (string.IsNullOrWhiteSpace(eligibleCategoryId) ||
                !_db.Categories.Any(c => c.CategoryId == eligibleCategoryId))
            {
                TempData["Error"] = "Please choose a valid category for the customization.";
                return RedirectToAction(nameof(Edit), new { id = menuItemId });
            }

            int ocSeq = GetNextOcNumber();
            var c = new OrderCustomizationSettings
            {
                MenuItemCustomizationId = FormatOcId(ocSeq, menuItemId),
                MenuItemId = menuItemId,
                CustomizationName = name.Trim(),
                CustomizationAdditionalPrice = price,
                CustomizationDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                EligibleCategoryId = eligibleCategoryId
            };

            _db.OrderCustomizationSettings.Add(c);
            _db.SaveChanges();
            TempData["Info"] = "Customization added.";
            return RedirectToAction(nameof(Edit), new { id = menuItemId });
        }

        // ------------------- HELPERS -------------------
        private List<SelectListItem> BuildCategorySelectList(string? selected = null, bool includePrompt = false)
        {
            var list = _db.Categories
                .OrderBy(c => c.CategoryId)
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId,
                    Text = $"{c.CategoryId} — {c.CategoryName}"
                })
                .ToList();

            if (includePrompt)
                list.Insert(0, new SelectListItem { Value = "", Text = "— Select a category —" });

            if (!string.IsNullOrWhiteSpace(selected))
            {
                var hit = list.FirstOrDefault(i => i.Value == selected);
                if (hit != null) hit.Selected = true;
            }
            return list;
        }

        private string GenerateMenuItemId_M3()
        {
            var ids = _db.MenuItems.Select(m => m.MenuItemId).ToList();
            int max = 0;
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id) || id.Length < 2 || char.ToUpperInvariant(id[0]) != 'M') continue;
                var numPart = id.Substring(1);
                if (int.TryParse(numPart, out var n) && n > max) max = n;
            }
            return $"M{(max + 1).ToString("D3", CultureInfo.InvariantCulture)}";
        }

        private int GetNextOcNumber()
        {
            var ids = _db.OrderCustomizationSettings.Select(c => c.MenuItemCustomizationId).ToList();
            int max = 1000;
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (id.Length < 4) continue;
                if (char.ToUpperInvariant(id[0]) != 'O' || char.ToUpperInvariant(id[1]) != 'C') continue;

                int underscore = id.IndexOf('_');
                string numeric = underscore > 2 ? id.Substring(2, underscore - 2) : id.Substring(2);

                if (int.TryParse(numeric, out var n) && n > max) max = n;
            }
            return max + 1;
        }

        private string FormatOcId(int ocNumber, string menuItemId)
        {
            return $"OC{ocNumber.ToString("D4", CultureInfo.InvariantCulture)}_{menuItemId}";
        }

        // ------------------- ADMIN: CATEGORIES -------------------
        [HttpGet]
        public IActionResult Categories(string? q)
        {
            var list = _db.Categories
                .Where(c => string.IsNullOrWhiteSpace(q) || c.CategoryName.Contains(q) || c.CategoryId.Contains(q))
                .OrderBy(c => c.CategoryId)
                .AsNoTracking()
                .ToList();

            ViewBag.FilterQ = q ?? "";
            return View(list);
        }

        [HttpGet]
        public IActionResult CreateCategory()
        {
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCategory(Category vm)
        {
            // ignore posted ID; we will generate
            ModelState.Remove(nameof(Category.CategoryId));

            if (string.IsNullOrWhiteSpace(vm.CategoryName))
                ModelState.AddModelError(nameof(vm.CategoryName), "CategoryName is required.");

            bool nameExists = _db.Categories
                .Any(c => c.CategoryName.ToLower() == vm.CategoryName.Trim().ToLower());
            if (nameExists)
                ModelState.AddModelError(nameof(vm.CategoryName), "A category with this name already exists.");

            if (!ModelState.IsValid) return View(vm);

            vm.CategoryId = GenerateCategoryId_C3();

            vm.CategoryName = vm.CategoryName.Trim();
            vm.CategoryDescription = string.IsNullOrWhiteSpace(vm.CategoryDescription) ? null : vm.CategoryDescription.Trim();

            _db.Categories.Add(vm);
            _db.SaveChanges();
            TempData["Info"] = $"Category '{vm.CategoryName}' created.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet]
        public IActionResult EditCategory(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var cat = _db.Categories.FirstOrDefault(c => c.CategoryId == id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCategory(Category vm)
        {
            if (string.IsNullOrWhiteSpace(vm.CategoryId)) return NotFound();

            var existing = _db.Categories.FirstOrDefault(c => c.CategoryId == vm.CategoryId);
            if (existing == null) return NotFound();

            if (string.IsNullOrWhiteSpace(vm.CategoryName))
                ModelState.AddModelError(nameof(vm.CategoryName), "CategoryName is required.");

            bool nameExists = _db.Categories
                .Any(c => c.CategoryId != vm.CategoryId &&
                          c.CategoryName.ToLower() == (vm.CategoryName ?? "").Trim().ToLower());
            if (nameExists)
                ModelState.AddModelError(nameof(vm.CategoryName), "A category with this name already exists.");

            if (!ModelState.IsValid) return View(vm);

            existing.CategoryName = vm.CategoryName.Trim();
            existing.CategoryDescription = string.IsNullOrWhiteSpace(vm.CategoryDescription) ? null : vm.CategoryDescription.Trim();

            _db.SaveChanges();
            TempData["Info"] = "Category updated.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet]
        public IActionResult DeleteCategory(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var cat = _db.Categories
                .Include(c => c.CategoryMenuItems)
                .FirstOrDefault(c => c.CategoryId == id);
            if (cat == null) return NotFound();

            if (cat.CategoryMenuItems != null && cat.CategoryMenuItems.Any())
                TempData["Error"] = "This category has menu items and cannot be deleted.";

            return View(cat);
        }

        [HttpPost, ActionName("DeleteCategory")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategoryConfirmed(string id)
        {
            var cat = _db.Categories
                .Include(c => c.CategoryMenuItems)
                .FirstOrDefault(c => c.CategoryId == id);
            if (cat == null) return NotFound();

            if (cat.CategoryMenuItems != null && cat.CategoryMenuItems.Any())
            {
                TempData["Error"] = "This category has menu items and cannot be deleted.";
                return RedirectToAction(nameof(Categories));
            }

            _db.Categories.Remove(cat);
            _db.SaveChanges();
            TempData["Info"] = "Category deleted.";
            return RedirectToAction(nameof(Categories));
        }

        // --- AJAX endpoint for inline modal creation on Menu Create page ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AjaxCreateCategory([FromForm] string name, [FromForm] string? description)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("CategoryName is required.");

            var exists = _db.Categories.Any(c => c.CategoryName.ToLower() == name.Trim().ToLower());
            if (exists)
                return Conflict("A category with this name already exists.");

            var cat = new Category
            {
                CategoryId = GenerateCategoryId_C3(),
                CategoryName = name.Trim(),
                CategoryDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
            };
            _db.Categories.Add(cat);
            _db.SaveChanges();

            return Json(new { id = cat.CategoryId, name = $"{cat.CategoryId} — {cat.CategoryName}" });
        }

        private string GenerateCategoryId_C3()
        {
            var ids = _db.Categories.Select(c => c.CategoryId).ToList();
            int max = 0;
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id) || id.Length < 2) continue;
                if (char.ToUpperInvariant(id[0]) != 'C') continue;
                var part = id.Substring(1);
                if (int.TryParse(part, out var n) && n > max) max = n;
            }
            return $"C{(max + 1).ToString("D3", CultureInfo.InvariantCulture)}";
        }

        // Save a base64 data URL (e.g. "data:image/jpeg;base64,...") into /wwwroot/images/menu
        private string SaveDataUrlToMenu(string dataUrl)
        {
            if (string.IsNullOrWhiteSpace(dataUrl)) return "/images/default.png";

            var comma = dataUrl.IndexOf(',');
            if (comma < 0) return "/images/default.png";

            var header = dataUrl.Substring(0, comma).ToLowerInvariant();
            var ext = header.Contains("image/png") ? ".png"
                    : header.Contains("image/webp") ? ".webp"
                    : ".jpg"; // default

            var base64 = dataUrl.Substring(comma + 1);
            var bytes = Convert.FromBase64String(base64);

            var webRoot = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

            var folderAbs = Path.Combine(webRoot, "images", "menu");
            Directory.CreateDirectory(folderAbs);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folderAbs, fileName);
            System.IO.File.WriteAllBytes(fullPath, bytes);

            return $"/images/menu/{fileName}";
        }

    }
}
