/*
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnomiAssignmentReal.Data;
using SnomiAssignmentReal.Helpers;
using SnomiAssignmentReal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Mail; // needed to build the message
using System.Globalization;
using System.Text.RegularExpressions;



namespace SnomiAssignmentReal.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _cfg;

        public OrderController(ApplicationDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _db = db;
            _env = env;
            _cfg = cfg;
        }


        // =========================================================
        // Helpers
        // =========================================================

        /// Ensure we have a CustomerId (logged-in OR guest). Create guest if missing.
        /// If logged in, use claim CustomerId and migrate ALL guest orders to it.
        private async Task<string> EnsureCustomerIdAsync()
        {
            // If authenticated, trust the CustomerId claim
            if (User?.Identity?.IsAuthenticated == true)
            {
                var claimCustomerId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("cid");

                if (!string.IsNullOrWhiteSpace(claimCustomerId))
                {
                    // If we previously used a guest, reassign ALL guest orders to this member id
                    var guestId = HttpContext.Session.GetString("GuestCustomerId");
                    if (!string.IsNullOrWhiteSpace(guestId) && guestId.StartsWith("G", StringComparison.OrdinalIgnoreCase))
                    {
                        var guestOrders = await _db.CustomerOrders
                            .Where(o => o.CustomerId == guestId)
                            .ToListAsync();

                        if (guestOrders.Count > 0)
                        {
                            foreach (var go in guestOrders) go.CustomerId = claimCustomerId;
                            await _db.SaveChangesAsync();
                        }

                        HttpContext.Session.Remove("GuestCustomerId");
                    }

                    HttpContext.Session.SetString("IsGuest", "false");
                    HttpContext.Session.SetString("CustomerId", claimCustomerId);
                    return claimCustomerId;
                }

                // Fallback (rare): look up by username/email if claim missing
                var username = User.Identity!.Name;
                var email = User.FindFirstValue(ClaimTypes.Email);

                var member = await _db.Customers
                    .FirstOrDefaultAsync(c =>
                        (!string.IsNullOrEmpty(username) && c.CustomerUserName == username) ||
                        (!string.IsNullOrEmpty(email) && c.CustomerEmailAddress == email));

                if (member != null)
                {
                    HttpContext.Session.SetString("IsGuest", "false");
                    HttpContext.Session.SetString("CustomerId", member.CustomerId);
                    HttpContext.Session.Remove("GuestCustomerId");
                    return member.CustomerId;
                }
            }

            // Not authenticated → keep or create guest
            var id = HttpContext.Session.GetString("CustomerId")
                  ?? HttpContext.Session.GetString("GuestCustomerId");
            if (!string.IsNullOrWhiteSpace(id)) return id;

            id = "G" + Guid.NewGuid().ToString("N")[..9];
            var guest = new Customer
            {
                CustomerId = id,
                CustomerFullName = "Guest",
                IsCustomerLoggedIn = false
            };
            _db.Customers.Add(guest);
            await _db.SaveChangesAsync();

            HttpContext.Session.SetString("IsGuest", "true");
            HttpContext.Session.SetString("GuestCustomerId", id);
            HttpContext.Session.SetString("CustomerId", id);

            return id;
        }

        /// Fetch or create the current "Cart" order for a customer.
        private async Task<CustomerOrder> EnsureOpenCartAsync(string customerId)
        {
            var order = await _db.CustomerOrders
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.MenuItem)
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.AppliedCustomizations)
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderStatus == "Cart");

            if (order != null) return order;

            order = new CustomerOrder
            {
                CustomerOrderId = "O" + Guid.NewGuid().ToString("N").Substring(0, 8),
                CustomerId = customerId,
                OrderStatus = "Cart",
                OrderCreatedAt = DateTime.Now,
                PaymentCompletedAt = null,
                OrderTotalAmount = 0m,
                CustomerOrderDetails = new List<OrderDetail>()
            };
            _db.CustomerOrders.Add(order);
            await _db.SaveChangesAsync();
            return order;
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers.TryGetValue("X-Requested-With", out var val)
                   && string.Equals(val, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }

        private static decimal ComputeTotal(CustomerOrder order)
        {
            return order.CustomerOrderDetails.Sum(od =>
            {
                var price = od.MenuItem?.MenuItemUnitPrice ?? 0m;
                var custom = od.AppliedCustomizations?.Sum(c => c.CustomizationAdditionalPrice) ?? 0m;
                return (price + custom) * od.OrderedQuantity;
            });
        }

        private static bool CustomSetEquals(
    IEnumerable<OrderCustomizationSettings>? existing,
    IEnumerable<string>? requestedIds)
        {
            var existingIds = (existing ?? Enumerable.Empty<OrderCustomizationSettings>())
                .Select(c => c.MenuItemCustomizationId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .OrderBy(id => id)
                .ToList();

            var reqIds = (requestedIds ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            return existingIds.SequenceEqual(reqIds);
        }


        // Allow customer cancel up to the moment the kitchen starts (Preparing+ is blocked)
        private static readonly HashSet<string> CustomerCancellableStatuses =
            new(StringComparer.OrdinalIgnoreCase) { "AwaitingPayment", "Ordered" };

        private static bool CanCustomerCancel(CustomerOrder order)
        {
            if (order == null) return false;
            var status = (order.OrderStatus ?? "").Trim();
            return CustomerCancellableStatuses.Contains(status);
        }

        // =========================================================
        // Cart
        // =========================================================

        // GET: /Order/Cart
        [HttpGet]
        public async Task<IActionResult> Cart()
        {
            var customerId = await EnsureCustomerIdAsync();
            var order = await _db.CustomerOrders
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.MenuItem)
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.AppliedCustomizations)
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderStatus == "Cart");

            if (order == null)
                order = await EnsureOpenCartAsync(customerId);

            return View(order);
        }

        // POST: /Order/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(string menuItemId, int quantity = 1, List<string>? customizationIds = null)
        {
            // Basic input check
            if (string.IsNullOrWhiteSpace(menuItemId))
            {
                var msg = "Menu item is required.";
                if (IsAjaxRequest()) return Json(new { ok = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction("Catalog", "Menu");
            }

            // Load item and validate availability
            var item = await _db.MenuItems
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MenuItemId == menuItemId);

            if (item == null)
            {
                var msg = "Menu item not found.";
                if (IsAjaxRequest()) return Json(new { ok = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction("Catalog", "Menu");
            }

            // 🚫 HARD GUARD: block ordering when unavailable
            if (!item.IsAvailableForOrder)
            {
                var msg = "This item is currently unavailable and can’t be ordered.";
                if (IsAjaxRequest()) return Json(new { ok = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction("Details", "Menu", new { id = menuItemId });
            }

            // Proceed as normal
            var customerId = await EnsureCustomerIdAsync();
            var order = await EnsureOpenCartAsync(customerId);
            if (quantity < 1) quantity = 1;

            // Normalise customisation IDs (for comparison)
            var normalizedIds = (customizationIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            // 🔍 Try to find an existing line with same item + same customisations
            var existingDetail = order.CustomerOrderDetails
                .FirstOrDefault(od =>
                    od.MenuItemId == menuItemId &&
                    CustomSetEquals(od.AppliedCustomizations, normalizedIds));

            if (existingDetail != null)
            {
                // ✅ Same line → just increase quantity
                existingDetail.OrderedQuantity += quantity;
            }
            else
            {
                // ➕ Different combo → create new line
                var detail = new OrderDetail
                {
                    OrderDetailId = "OD" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    CustomerOrderId = order.CustomerOrderId,
                    MenuItemId = menuItemId,
                    OrderedQuantity = quantity,
                    AppliedCustomizations = new List<OrderCustomizationSettings>()
                };

                if (normalizedIds.Any())
                {
                    var selected = await _db.OrderCustomizationSettings
                        .Where(c => normalizedIds.Contains(c.MenuItemCustomizationId))
                        .ToListAsync();
                    detail.AppliedCustomizations = selected;
                }

                order.CustomerOrderDetails.Add(detail);
            }

            await _db.SaveChangesAsync();

            if (IsAjaxRequest())
            {
                var fresh = await _db.CustomerOrders
                    .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.MenuItem)
                    .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.AppliedCustomizations)
                    .FirstOrDefaultAsync(o => o.CustomerOrderId == order.CustomerOrderId);

                var count = fresh?.CustomerOrderDetails?.Sum(od => od.OrderedQuantity) ?? 0;
                var total = fresh != null ? ComputeTotal(fresh) : 0m;

                return Json(new
                {
                    ok = true,
                    message = "Added to cart.",
                    cartCount = count,
                    cartTotal = total.ToString("C")
                });
            }

            TempData["CartMessage"] = "Added to cart.";
            return RedirectToAction(nameof(Cart));
        }


        // POST: /Order/RemoveFromCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(string orderDetailId)
        {
            var customerId = await EnsureCustomerIdAsync();
            var order = await _db.CustomerOrders
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderStatus == "Cart");

            if (order == null)
            {
                if (IsAjaxRequest()) return Json(new { ok = false, message = "Cart not found." });
                TempData["Error"] = "Cart not found.";
                return RedirectToAction(nameof(Cart));
            }

            var detail = await _db.OrderDetails
                .Include(d => d.AppliedCustomizations)
                .FirstOrDefaultAsync(d => d.OrderDetailId == orderDetailId && d.CustomerOrderId == order.CustomerOrderId);

            if (detail != null)
            {
                detail.AppliedCustomizations?.Clear();
                _db.OrderDetails.Remove(detail);
                await _db.SaveChangesAsync();
            }

            if (IsAjaxRequest()) return Json(new { ok = true, message = "Item removed." });
            TempData["CartMessage"] = "Item removed.";
            return RedirectToAction(nameof(Cart));
        }

        // POST: /Order/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(string orderDetailId, int quantity)
        {
            var customerId = await EnsureCustomerIdAsync();
            var order = await _db.CustomerOrders
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderStatus == "Cart");

            if (order == null)
            {
                if (IsAjaxRequest()) return Json(new { ok = false, message = "Cart not found." });
                TempData["Error"] = "Cart not found.";
                return RedirectToAction(nameof(Cart));
            }

            var detail = await _db.OrderDetails
                .Include(d => d.AppliedCustomizations)
                .FirstOrDefaultAsync(d => d.OrderDetailId == orderDetailId && d.CustomerOrderId == order.CustomerOrderId);

            if (detail == null)
            {
                if (IsAjaxRequest()) return Json(new { ok = false, message = "Item not found." });
                TempData["Error"] = "Item not found.";
                return RedirectToAction(nameof(Cart));
            }

            if (quantity <= 0)
            {
                detail.AppliedCustomizations?.Clear();
                _db.OrderDetails.Remove(detail);
                await _db.SaveChangesAsync();
                if (IsAjaxRequest()) return Json(new { ok = true, message = "Item removed." });
                TempData["CartMessage"] = "Item removed.";
            }
            else
            {
                detail.OrderedQuantity = quantity;
                await _db.SaveChangesAsync();
                if (IsAjaxRequest()) return Json(new { ok = true, message = "OrderedQuantity updated." });
                TempData["CartMessage"] = "OrderedQuantity updated.";
            }

            return RedirectToAction(nameof(Cart));
        }
        // ======================
        // EDIT CUSTOMISATIONS (GET)
        // ======================
        [HttpGet]
        public async Task<IActionResult> EditCustomizations(string orderDetailId)
        {
            if (string.IsNullOrWhiteSpace(orderDetailId))
                return BadRequest();

            var customerId = await EnsureCustomerIdAsync();

            var detail = await _db.OrderDetails
                .Include(d => d.CustomerOrder)
                .Include(d => d.MenuItem)
                .Include(d => d.AppliedCustomizations)
                .FirstOrDefaultAsync(d => d.OrderDetailId == orderDetailId);

            if (detail == null)
                return NotFound();

            // Must belong to current customer and still be in Cart
            if (!string.Equals(detail.CustomerOrder.CustomerId, customerId, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            if (!string.Equals(detail.CustomerOrder.OrderStatus, "Cart", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "You can only edit customisations for items that are still in your cart.";
                return RedirectToAction(nameof(Cart));
            }

            var allCustoms = await _db.OrderCustomizationSettings
                .Where(c => c.MenuItemId == detail.MenuItemId)
                .OrderBy(c => c.CustomizationAdditionalPrice)
                .ThenBy(c => c.CustomizationName)
                .ToListAsync();

            ViewBag.AllCustomizations = allCustoms;

            // USE THE RENAMED VIEW HERE
            return View("EditCartCustomizations", detail);
        }

        // ======================
        // EDIT CUSTOMISATIONS (POST)
        // ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomizations(string orderDetailId, List<string> selectedCustomizations)
        {
            if (string.IsNullOrWhiteSpace(orderDetailId))
                return BadRequest();

            var customerId = await EnsureCustomerIdAsync();

            var detail = await _db.OrderDetails
                .Include(d => d.CustomerOrder)
                .Include(d => d.AppliedCustomizations)
                .FirstOrDefaultAsync(d => d.OrderDetailId == orderDetailId);

            if (detail == null)
                return NotFound();

            // Must belong to current customer and still be in Cart
            if (!string.Equals(detail.CustomerOrder.CustomerId, customerId, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            if (!string.Equals(detail.CustomerOrder.OrderStatus, "Cart", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "You can only edit customisations for items that are still in your cart.";
                return RedirectToAction(nameof(Cart));
            }

            // Clear existing links
            detail.AppliedCustomizations?.Clear();

            // Apply new selection
            if (selectedCustomizations != null && selectedCustomizations.Any())
            {
                var newCustoms = await _db.OrderCustomizationSettings
                    .Where(c => selectedCustomizations.Contains(c.MenuItemCustomizationId)
                                && c.MenuItemId == detail.MenuItemId)
                    .ToListAsync();

                detail.AppliedCustomizations = newCustoms;
            }

            await _db.SaveChangesAsync();

            TempData["CartMessage"] = "Customisations updated.";
            return RedirectToAction(nameof(Cart));
        }


        // =========================================================
        // Checkout (with QR branch)
        // =========================================================

        // GET: /Order/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var customerId = await EnsureCustomerIdAsync();

            var order = await _db.CustomerOrders
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.MenuItem)
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.AppliedCustomizations)
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && (o.OrderStatus == "Cart" || o.OrderStatus == "AwaitingPayment"));

            // ❌ (Removed old "stray guest cart" block; EnsureCustomerIdAsync() handles all migration)

            if (order == null || order.CustomerOrderDetails == null || !order.CustomerOrderDetails.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction(nameof(Cart));
            }

            // --- Points balance: always take from the member row if authenticated ---
            int balance = 0;
            if (User?.Identity?.IsAuthenticated == true)
            {
                var username = User.Identity!.Name;
                var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

                var member = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c =>
                    (!string.IsNullOrEmpty(username) && c.CustomerUserName == username) ||
                    (!string.IsNullOrEmpty(email) && c.CustomerEmailAddress == email) ||
                    c.CustomerId == order.CustomerId); // fallback
                balance = member?.CustomerRewardPoints ?? 0;
            }
            else if (!string.IsNullOrEmpty(order.CustomerId) && !order.CustomerId.StartsWith("G"))
            {
                var member = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == order.CustomerId);
                balance = member?.CustomerRewardPoints ?? 0;
            }

            ViewBag.MemberPoints = balance;
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string paymentMethod, int tableNo = 0, int pointsToRedeem = 0)
        {
            var customerId = await EnsureCustomerIdAsync();
            var order = await _db.CustomerOrders
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.MenuItem)
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.AppliedCustomizations)
                .FirstOrDefaultAsync(o =>
                    o.CustomerId == customerId &&
                    (o.OrderStatus == "Cart" || o.OrderStatus == "AwaitingPayment"));

            if (order == null || order.CustomerOrderDetails == null || !order.CustomerOrderDetails.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction(nameof(Cart));
            }

            // Always recompute amount
            var total = ComputeTotal(order);
            order.OrderTotalAmount = total;

            // Identify member (for redemption)
            Customer? member = null;
            if (!string.IsNullOrEmpty(order.CustomerId) && !order.CustomerId.StartsWith("G"))
                member = await _db.Customers
    .AsTracking()      // 🔥 forces EF to load the LATEST customer
    .FirstOrDefaultAsync(c => c.CustomerId == order.CustomerId);


            // Normalize + whitelist check
            var method = (paymentMethod ?? string.Empty).Trim();
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Cash", "Card", "E-Wallet", "QR" };

            if (string.IsNullOrWhiteSpace(method))
                ModelState.AddModelError("paymentMethod", "Please choose a payment method.");

            if (!string.IsNullOrWhiteSpace(method) && !allowed.Contains(method))
                ModelState.AddModelError("paymentMethod", "Invalid payment method.");

            // Table number guard (optional input)
            if (tableNo < 0 || tableNo > 100)
                ModelState.AddModelError("tableNo", "Table number must be between 0 and 100.");

            // Validate Card fields ONLY when Card is chosen
            if (method.Equals("Card", StringComparison.OrdinalIgnoreCase))
            {
                var cardName = Request.Form["cardName"].ToString();
                var cardNumber = (Request.Form["cardNumber"].ToString() ?? "").Replace(" ", "");
                var cardExp = Request.Form["cardExp"].ToString();
                var cardCvv = Request.Form["cardCvv"].ToString();

                // Letters/spaces/.'- only (Unicode letters allowed), 2–50 chars
                var nameOk = Regex.IsMatch(cardName ?? "", @"^[\p{L}\p{M}' .-]{2,50}$", RegexOptions.Compiled);
                if (!nameOk)
                    ModelState.AddModelError("cardName", "CategoryName must be letters only (2–50 characters).");

                if (string.IsNullOrWhiteSpace(cardNumber) || !cardNumber.All(char.IsDigit) || cardNumber.Length < 12 || cardNumber.Length > 19)
                    ModelState.AddModelError("cardNumber", "Enter a valid card number (12–19 digits).");

                if (!Regex.IsMatch(cardExp ?? "", @"^(0[1-9]|1[0-2])\/\d{2}$"))
                {
                    ModelState.AddModelError("cardExp", "Use MM/YY format.");
                }
                else
                {
                    // Additional expiry validation
                    var parts = cardExp.Split('/');
                    int mm = int.Parse(parts[0]);
                    int yy = int.Parse(parts[1]) + 2000;

                    var lastDay = DateTime.DaysInMonth(yy, mm);
                    var expiryDate = new DateTime(yy, mm, lastDay, 23, 59, 59);

                    if (expiryDate < DateTime.Now)
                        ModelState.AddModelError("cardExp", "This card is expired.");
                }


                if (!Regex.IsMatch(cardCvv ?? "", @"^\d{3,4}$"))
                    ModelState.AddModelError("cardCvv", "CVV must be 3–4 digits.");
            }

            if (method.Equals("E-Wallet", StringComparison.OrdinalIgnoreCase))
            {
                var provider = Request.Form["ewalletProvider"].ToString();
                var mobile = Request.Form["ewalletMobile"].ToString();

                if (string.IsNullOrWhiteSpace(provider))
                    ModelState.AddModelError("ewalletProvider", "Choose an e-wallet provider.");

                if (!Regex.IsMatch(mobile ?? "", @"^\+?\d{9,15}$"))
                    ModelState.AddModelError("ewalletMobile", "Enter a valid mobile number (9–15 digits).");
            }

            // ===== Points validation (authoritative) =====
            if (pointsToRedeem > 0)
            {
                if (member == null || member.CustomerRewardPoints <= 0)
                {
                    ModelState.AddModelError("pointsToRedeem", "You don’t have enough points to redeem.");
                }
                else if (pointsToRedeem > member.CustomerRewardPoints)
                {
                    ModelState.AddModelError("pointsToRedeem", $"You can redeem up to {member.CustomerRewardPoints} points.");
                }
            }

            // If anything invalid, redisplay the page with errors
            if (!ModelState.IsValid)
            {
                ViewBag.PaymentMethod = method; // keep selected
                ViewBag.TableNo = tableNo;
                ViewBag.MemberPoints = member?.CustomerRewardPoints ?? 0; // keep balance for the view
                return View(order);
            }

            // Persist chosen meta
            order.PaymentMethodName = method;
            order.TableNumber = tableNo;

            // ===== Apply reward redemption (snapshot onto order) =====
            int usedPts = 0;
            decimal discount = 0m;
            decimal net = total;

            if (member != null && pointsToRedeem > 0)
            {
                var res = ApplyPointsRedemption(member, total, pointsToRedeem);
                usedPts = res.pointsUsed;
                discount = res.discountApplied;
                net = res.netAmount;
            }

            order.RewardPointsRedeemed = usedPts;
            order.TotalDiscountAmount = discount;
            order.NetPayableAmount = net;

            // ===== QR branch: wait for confirm to award points =====
            if (method.Equals("QR", StringComparison.OrdinalIgnoreCase))
            {
                order.OrderStatus = "AwaitingPayment";
                order.PaymentCompletedAt = null;
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(QrPay), new { id = order.CustomerOrderId });
            }

            // ===== Non-QR: finalize immediately (award points safely) =====
            await FinalizePaymentAndRewardsAsync(order);
            await SendReceiptIfPossibleAsync(order);


            HttpContext.Session.SetString("LastOrderId", order.CustomerOrderId);
            return RedirectToAction(nameof(Track), new { id = order.CustomerOrderId });
        }

        // =========================================================
        // QR PAY FLOW
        // =========================================================

        // GET: /Order/QrPay/{id}
        [HttpGet]
        public async Task<IActionResult> QrPay(string id)
        {
            var order = await _db.CustomerOrders
                .Include(o => o.CustomerOrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .Include(o => o.CustomerOrderDetails)
                    .ThenInclude(od => od.AppliedCustomizations)   // ✅ FIX ADDED
                .FirstOrDefaultAsync(o => o.CustomerOrderId == id);

            if (order == null) return NotFound();
            if (!string.Equals(order.OrderStatus, "AwaitingPayment", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(Track), new { id });

            var amountForQr = order.NetPayableAmount > 0 ? order.NetPayableAmount : order.OrderTotalAmount;
            var payload = $"SNOMI|ORDER={order.CustomerOrderId}|AMOUNT={amountForQr:0.00}|CURRENCY=MYR";

            ViewBag.QrImage = Url.Content("~/images/qr-scan.png");
            ViewBag.Payload = payload;

            return View(order);
        }


        // POST: /Order/ConfirmQrPayment/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmQrPayment(string id)
        {
            var order = await _db.CustomerOrders
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.MenuItem)
                .FirstOrDefaultAsync(o => o.CustomerOrderId == id);
            if (order == null) return NotFound();

            if (string.Equals(order.OrderStatus, "AwaitingPayment", StringComparison.OrdinalIgnoreCase))
            {
                await FinalizePaymentAndRewardsAsync(order);
                await SendReceiptIfPossibleAsync(order);   // ✅ add this
            }

            HttpContext.Session.SetString("LastOrderId", order.CustomerOrderId);
            return RedirectToAction(nameof(Track), new { id = order.CustomerOrderId });
        }


        // =========================================================
        // Tracking (Customer & Staff)
        // =========================================================

        // GET: /Order/Track/{id}
        [HttpGet("Order/Track/{id}")]
        public async Task<IActionResult> Track(string id)
        {
            // Ensure any guest→member migration runs before we check ownership
            var currentCid = await EnsureCustomerIdAsync();

            var order = await _db.CustomerOrders
                .Include(o => o.Customer)
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.MenuItem)
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.AppliedCustomizations)
                .FirstOrDefaultAsync(o => o.CustomerOrderId == id);

            if (order == null) return NotFound();

            // Allow Admin/Staff to view any order; customers must own it
            var isStaff = User.IsInRole("Admin") || User.IsInRole("Staff");
            if (!isStaff && !string.Equals(order.CustomerId, currentCid, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "That order isn’t linked to your account.";
                return RedirectToAction(nameof(MyOrders));
            }

            return View(order);
        }


        // Quick redirect to ManageOrders (prevents AllOrders view error)
        [HttpGet]
        public IActionResult AllOrders() => RedirectToAction(nameof(ManageOrders));

        // =========================================================
        // Admin/Staff – Manage CustomerOrders
        // =========================================================

        // GET: /Order/ManageOrders
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ManageOrders(string? status, string? q, int page = 1, int pageSize = 12)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 5 or > 100 ? 12 : pageSize;

            var qry = _db.CustomerOrders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.CustomerOrderDetails).ThenInclude(d => d.MenuItem)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                qry = qry.Where(o => o.OrderStatus == status);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                qry = qry.Where(o =>
                    o.CustomerOrderId.Contains(term) ||
                    (o.Customer != null && (o.Customer.CustomerFullName.Contains(term) || o.Customer.CustomerId.Contains(term))) ||
                    (o.PaymentMethodName ?? "").Contains(term) ||
                    o.TableNumber.ToString().Contains(term));
            }

            var total = await qry.CountAsync();

            var orders = await qry
                .OrderByDescending(o => o.OrderCreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new SnomiAssignmentReal.Models.ViewModels.ManageOrdersVm
            {
                Orders = orders,
                Q = q,
                Status = status,
                Page = page,
                PageSize = pageSize,
                Total = total
            };

            return View(vm); // Views/Order/ManageOrders.cshtml
        }


        // GET: /Order/ManageOrderHistory
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ManageOrderHistory(
            string? q,
            string status = "all",         // all | served | completed | cancelled
            string paid = "all",           // all | yes | no
            string method = "all",         // all | Cash | Card | E-Wallet | QR
            string customerType = "all",   // all | guest | registered
            DateTime? from = null,
            DateTime? to = null,
            decimal? minTotal = null,
            decimal? maxTotal = null,
            string sort = "recent_desc",
            int page = 1,
            int pageSize = 12)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 5 or > 100 ? 12 : pageSize;

            var allowedHistory = new[] { "Served", "Completed", "Cancelled" };

            var qry = _db.CustomerOrders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.MenuItem)
                .Where(o => allowedHistory.Contains(o.OrderStatus))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                qry = qry.Where(o =>
                    o.CustomerOrderId.Contains(term) ||
                    (o.Customer != null && (o.Customer.CustomerFullName.Contains(term) || o.Customer.CustomerId.Contains(term))) ||
                    (o.PaymentMethodName ?? "").Contains(term) ||
                    o.TableNumber.ToString().Contains(term));
            }

            switch ((status ?? "all").ToLower())
            {
                case "served": qry = qry.Where(o => o.OrderStatus == "Served"); break;
                case "completed": qry = qry.Where(o => o.OrderStatus == "Completed"); break;
                case "cancelled": qry = qry.Where(o => o.OrderStatus == "Cancelled"); break;
            }

            switch ((paid ?? "all").ToLower())
            {
                case "yes": qry = qry.Where(o => o.PaymentCompletedAt != null); break;
                case "no": qry = qry.Where(o => o.PaymentCompletedAt == null); break;
            }

            if (!string.IsNullOrWhiteSpace(method) && !string.Equals(method, "all", StringComparison.OrdinalIgnoreCase))
                qry = qry.Where(o => (o.PaymentMethodName ?? "") == method);

            switch ((customerType ?? "all").ToLowerInvariant())
            {
                case "guest":
                    qry = qry.Where(o =>
                        // guest by id prefix
                        (o.CustomerId != null && o.CustomerId.StartsWith("G")) ||
                        // or guest by customer record (no login fields)
                        (o.Customer != null && o.Customer.CustomerUserName == null && o.Customer.CustomerPasswordHash == null)
                    );
                    break;

                case "registered":
                    qry = qry.Where(o =>
                        // registered if not guest prefix and not null
                        (o.CustomerId != null && !o.CustomerId.StartsWith("G")) ||
                        // or registered by having login fields
                        (o.Customer != null && (o.Customer.CustomerUserName != null || o.Customer.CustomerPasswordHash != null))
                    );
                    break;
            }


            if (from.HasValue) qry = qry.Where(o => o.OrderCreatedAt >= from.Value);
            if (to.HasValue) qry = qry.Where(o => o.OrderCreatedAt <= to.Value);

            // choose NetPayableAmount if present, else OrderTotalAmount
            if (minTotal.HasValue) qry = qry.Where(o => (o.NetPayableAmount > 0 ? o.NetPayableAmount : (o.OrderTotalAmount - o.TotalDiscountAmount)) >= minTotal.Value);
            if (maxTotal.HasValue) qry = qry.Where(o => (o.NetPayableAmount > 0 ? o.NetPayableAmount : (o.OrderTotalAmount - o.TotalDiscountAmount)) <= maxTotal.Value);

            // Sorting
            qry = (sort ?? "recent_desc") switch
            {
                "recent_asc" => qry.OrderBy(o => o.OrderCreatedAt),
                "amount_desc" => qry.OrderByDescending(o => (o.NetPayableAmount > 0 ? o.NetPayableAmount : (o.OrderTotalAmount - o.TotalDiscountAmount))),
                "amount_asc" => qry.OrderBy(o => (o.NetPayableAmount > 0 ? o.NetPayableAmount : (o.OrderTotalAmount - o.TotalDiscountAmount))),
                "items_desc" => qry.OrderByDescending(o => o.CustomerOrderDetails.Sum(d => d.OrderedQuantity)).ThenByDescending(o => o.OrderCreatedAt),
                "items_asc" => qry.OrderBy(o => o.CustomerOrderDetails.Sum(d => d.OrderedQuantity)).ThenByDescending(o => o.OrderCreatedAt),
                _ => qry.OrderByDescending(o => o.OrderCreatedAt)
            };

            var total = await qry.CountAsync();
            var orders = await qry.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var vm = new SnomiAssignmentReal.Models.ViewModels.ManageOrderHistoryVm
            {
                Orders = orders,
                Q = q,
                Status = status,
                Paid = paid,
                Method = method,
                CustomerType = customerType,
                From = from,
                To = to,
                MinTotal = minTotal,
                MaxTotal = maxTotal,
                Sort = sort,
                Page = page,
                PageSize = pageSize,
                Total = total
            };

            return View("ManageOrderHistory", vm);
        }


        // GET: /Order/ManageTracks
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ManageTracks()
        {
            var activeStatuses = new[] { "AwaitingPayment", "Ordered", "Preparing", "Ready", "Served" };

            var orders = await _db.CustomerOrders
                .Include(o => o.Customer)
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.MenuItem)
                .Where(o => activeStatuses.Contains(o.OrderStatus))
                .OrderByDescending(o => o.OrderCreatedAt)
                .ToListAsync();

            return View("ManageTracks", orders); // Views/Order/ManageTracks.cshtml
        }

        // =========================================================
        // Admin/Staff – Mutations (OrderStatus/Payment)
        // =========================================================
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string id, string status)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(status))
                return BadRequest();

            var order = await _db.CustomerOrders.FirstOrDefaultAsync(o => o.CustomerOrderId == id);
            if (order == null) return NotFound();

            var oldStatus = (order.OrderStatus ?? "").Trim();
            var newStatus = status.Trim();

            // =========================================
            // SPECIAL CASE: Admin sets status = Cancelled
            // =========================================
            if (newStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                // Snapshot reward values before rollback (for the message)
                var usedBefore = order.RewardPointsRedeemed;
                var earnedBefore = order.RewardPointsEarned;

                // Undo payment & points
                var balanceTouched = await RollbackPaymentAndRewardsAsync(order, updateSessionIfCurrentUser: false);

                // Mark as cancelled
                order.OrderStatus = "Cancelled";
                await _db.SaveChangesAsync();

                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        ok = true,
                        status = order.OrderStatus,
                        usedReturned = balanceTouched ? usedBefore : 0,
                        earnedRemoved = balanceTouched ? earnedBefore : 0
                    });
                }

                // Build a human message for the admin
                if (balanceTouched && (usedBefore > 0 || earnedBefore > 0))
                {
                    if (usedBefore > 0 && earnedBefore > 0)
                    {
                        TempData["Info"] =
                            $"Order {order.CustomerOrderId} cancelled. {usedBefore} used points were returned, and {earnedBefore} earned points were removed from the customer’s balance.";
                    }
                    else if (usedBefore > 0)
                    {
                        TempData["Info"] =
                            $"Order {order.CustomerOrderId} cancelled. {usedBefore} used points were returned to the customer’s balance.";
                    }
                    else // only earned points
                    {
                        TempData["Info"] =
                            $"Order {order.CustomerOrderId} cancelled. {earnedBefore} earned points from this order were removed from the customer’s balance.";
                    }
                }
                else
                {
                    TempData["Info"] = $"Order {order.CustomerOrderId} cancelled.";
                }

                return RedirectToAction(nameof(ManageOrders));
            }


            // ===============================
            // Normal non-cancel flow
            // ===============================
            order.OrderStatus = newStatus;

            // If moving away from AwaitingPayment, treat as paid & send receipt
            if (oldStatus.Equals("AwaitingPayment", StringComparison.OrdinalIgnoreCase) &&
                !order.OrderStatus.Equals("AwaitingPayment", StringComparison.OrdinalIgnoreCase))
            {
                await FinalizePaymentAndRewardsAsync(order); // sets PaymentCompletedAt, points, etc.
                await SendReceiptIfPossibleAsync(order);
            }

            // If explicitly marked Completed and PaymentCompletedAt wasn't set, set it
            if (string.Equals(newStatus, "Completed", StringComparison.OrdinalIgnoreCase) &&
                order.PaymentCompletedAt == null)
            {
                order.PaymentCompletedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();

            if (IsAjaxRequest()) return Json(new { ok = true, status = order.OrderStatus });
            TempData["Info"] = $"Order {order.CustomerOrderId} set to {order.OrderStatus}.";
            return RedirectToAction(nameof(ManageOrders));
        }



        // POST: /Order/AdvanceStatus
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdvanceStatus(string id)
        {
            var order = await _db.CustomerOrders.FirstOrDefaultAsync(o => o.CustomerOrderId == id);
            if (order == null) return NotFound();

            var oldStatus = (order.OrderStatus ?? "").Trim();
            var flow = new[] { "Ordered", "Preparing", "Ready", "Served", "Completed" };
            var idx = Array.IndexOf(flow, oldStatus);
            if (idx < 0) order.OrderStatus = "Ordered";
            else if (idx < flow.Length - 1) order.OrderStatus = flow[idx + 1];

            // If we advanced away from AwaitingPayment, finalize + send receipt
            if (oldStatus.Equals("AwaitingPayment", StringComparison.OrdinalIgnoreCase))
            {
                await FinalizePaymentAndRewardsAsync(order); // sets PaymentCompletedAt if needed
                await SendReceiptIfPossibleAsync(order);      // ✅ add this
            }

            if (string.Equals(order.OrderStatus, "Completed", StringComparison.OrdinalIgnoreCase) && order.PaymentCompletedAt == null)
                order.PaymentCompletedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            if (IsAjaxRequest()) return Json(new { ok = true, status = order.OrderStatus });
            TempData["Info"] = $"Order {order.CustomerOrderId} advanced to {order.OrderStatus}.";
            return RedirectToAction(nameof(ManageOrders));
        }


        // POST: /Order/MarkPaid
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(string id)
        {
            var order = await _db.CustomerOrders
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.MenuItem)
                .FirstOrDefaultAsync(o => o.CustomerOrderId == id);
            if (order == null) return NotFound();

            // Award points & finalize idempotently
            await FinalizePaymentAndRewardsAsync(order);
            await SendReceiptIfPossibleAsync(order);

            if (IsAjaxRequest()) return Json(new { ok = true, paid = order.PaymentCompletedAt, pointsAwardedAt = order.RewardPointsAwardedAt });
            TempData["Info"] = $"Order {order.CustomerOrderId} marked as paid.";
            return RedirectToAction(nameof(ManageOrders));
        }
        // POST: /Order/Cancel  (Admin/Staff)
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var order = await _db.CustomerOrders
                .Include(o => o.CustomerOrderDetails)
                .FirstOrDefaultAsync(o => o.CustomerOrderId == id);

            if (order == null) return NotFound();

            // Snapshot current reward values (for the message)
            var usedBefore = order.RewardPointsRedeemed;
            var earnedBefore = order.RewardPointsEarned;

            // 🔁 Undo payment & points
            var balanceTouched = await RollbackPaymentAndRewardsAsync(order, updateSessionIfCurrentUser: false);

            // Mark as cancelled
            order.OrderStatus = "Cancelled";
            await _db.SaveChangesAsync();

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    ok = true,
                    status = order.OrderStatus,
                    usedReturned = balanceTouched ? usedBefore : 0,
                    earnedRemoved = balanceTouched ? earnedBefore : 0
                });
            }

            // Build a human message for the admin
            if (balanceTouched && (usedBefore > 0 || earnedBefore > 0))
            {
                if (usedBefore > 0 && earnedBefore > 0)
                {
                    TempData["Info"] =
                        $"Order {order.CustomerOrderId} cancelled. {usedBefore} used points were returned, and {earnedBefore} earned points were removed from the customer’s balance.";
                }
                else if (usedBefore > 0)
                {
                    TempData["Info"] =
                        $"Order {order.CustomerOrderId} cancelled. {usedBefore} used points were returned to the customer’s balance.";
                }
                else // only earned points
                {
                    TempData["Info"] =
                        $"Order {order.CustomerOrderId} cancelled. {earnedBefore} earned points from this order were removed from the customer’s balance.";
                }
            }
            else
            {
                TempData["Info"] = $"Order {order.CustomerOrderId} cancelled.";
            }

            return RedirectToAction(nameof(ManageOrders));
        }


        // POST: /Order/CancelMyOrder  (Customer/Guest)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelMyOrder(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var customerId = await EnsureCustomerIdAsync();
            var order = await _db.CustomerOrders
                .FirstOrDefaultAsync(o => o.CustomerOrderId == id);

            if (order == null) return NotFound();

            // Must be their own order
            if (!string.Equals(order.CustomerId, customerId, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            if (!CanCustomerCancel(order))
            {
                var status = order.OrderStatus ?? "in progress";
                TempData["Error"] =
                    $"We have already started {status} your order. Online cancellation is no longer available. Please ask a staff member.";
                return RedirectToAction(nameof(Track), new { id });
            }

            var usedBefore = order.RewardPointsRedeemed;
            var earnedBefore = order.RewardPointsEarned;

            var balanceTouched = await RollbackPaymentAndRewardsAsync(order, updateSessionIfCurrentUser: true);

            order.OrderStatus = "Cancelled";
            await _db.SaveChangesAsync();

            if (balanceTouched && (usedBefore > 0 || earnedBefore > 0))
            {
                TempData["Info"] =
                    "Your order has been cancelled. Any reward points you used for this order have been returned and any points earned from it have been removed.";
            }
            else
            {
                TempData["Info"] = "Your order has been cancelled.";
            }

            return RedirectToAction(nameof(Track), new { id });
        }




        // =========================================================
        // Rewards helpers (private)
        // =========================================================

        private (int pointsUsed, decimal discountApplied, decimal netAmount)
            ApplyPointsRedemption(Customer? customer, decimal subtotal, int requestedPoints)
        {
            // Guests or no member -> no redemption
            if (customer == null || string.IsNullOrWhiteSpace(customer.CustomerId) || customer.CustomerId.StartsWith("G"))
                return (0, 0m, subtotal);

            var usablePoints = Math.Max(0, Math.Min(requestedPoints, customer.CustomerRewardPoints));
            if (usablePoints == 0 || subtotal <= 0) return (0, 0m, subtotal);

            var requestedDiscount = RewardPoints.ComputeDiscountForPoints(usablePoints);

            // Cap to 50% (configurable)
            var cap = Math.Round(subtotal * RewardPoints.MaxRedeemPctOfSubtotal, 2, MidpointRounding.AwayFromZero);
            var discount = Math.Min(requestedDiscount, cap);

            // Convert the real discount back to exact points needed
            var pointsNeeded = RewardPoints.ComputePointsNeededForDiscount(discount);
            pointsNeeded = Math.Min(pointsNeeded, usablePoints);

            var net = subtotal - discount;
            if (net < 0) net = 0;

            return (pointsNeeded, discount, net);
        }

        private async Task FinalizePaymentAndRewardsAsync(CustomerOrder order)
        {
            if (order == null) return;

            // ✅ Already fully finalized? Do nothing (prevents double-deduct & double-award)
            if (order.PaymentCompletedAt != null && order.RewardPointsAwardedAt != null)
                return;

            // Mark paid & status
            if (order.PaymentCompletedAt == null)
                order.PaymentCompletedAt = DateTime.Now;

            if (!string.Equals(order.OrderStatus, "Ordered", StringComparison.OrdinalIgnoreCase))
                order.OrderStatus = "Ordered";

            // Members only, and only award once (RewardPointsAwardedAt == null)
            if (!string.IsNullOrEmpty(order.CustomerId) &&
                !order.CustomerId.StartsWith("G", StringComparison.OrdinalIgnoreCase))
            {
                var member = await _db.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == order.CustomerId);

                if (member != null && order.RewardPointsAwardedAt == null)
                {
                    // 1) Deduct redeemed points (if any were reserved for this order)
                    if (order.RewardPointsRedeemed > 0 &&
                        member.CustomerRewardPoints >= order.RewardPointsRedeemed)
                    {
                        member.CustomerRewardPoints -= order.RewardPointsRedeemed;
                    }

                    // 2) Earn points on net amount
                    var basis = order.NetPayableAmount > 0
                        ? order.NetPayableAmount
                        : (order.OrderTotalAmount - order.TotalDiscountAmount);

                    if (basis < 0) basis = 0;

                    var earned = RewardPoints.ComputeEarnedPoints(basis);
                    order.RewardPointsEarned = earned;
                    member.CustomerRewardPoints += earned;

                    // keep session/header in sync
                    HttpContext.Session.SetString("CustomerId", member.CustomerId);
                    HttpContext.Session.SetString("CustomerPoints", member.CustomerRewardPoints.ToString());

                    _db.Customers.Update(member);
                }
            }

            // mark snapshot so we never award again
            if (order.RewardPointsAwardedAt == null)
                order.RewardPointsAwardedAt = DateTime.UtcNow;

            _db.CustomerOrders.Update(order);
            await _db.SaveChangesAsync();
        }


        /// <summary>
        /// Roll back payment + reward points that were applied for this order.
        /// Returns true if the customer's point balance was actually changed.
        /// </summary>
        private async Task<bool> RollbackPaymentAndRewardsAsync(
    CustomerOrder order,
    bool updateSessionIfCurrentUser = false)
        {
            if (order == null) return false;

            bool touchedBalance = false;
            Customer? member = null;

            if (!string.IsNullOrWhiteSpace(order.CustomerId) &&
                !order.CustomerId.StartsWith("G", StringComparison.OrdinalIgnoreCase))
            {
                member = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == order.CustomerId);

                if (member != null)
                {
                    // 1) Give back redeemed points
                    if (order.RewardPointsRedeemed > 0)
                    {
                        member.CustomerRewardPoints += order.RewardPointsRedeemed;
                        touchedBalance = true;
                    }

                    // 2) Remove points earned
                    if (order.RewardPointsEarned > 0)
                    {
                        var newBalance = member.CustomerRewardPoints - order.RewardPointsEarned;
                        member.CustomerRewardPoints = newBalance < 0 ? 0 : newBalance;
                        touchedBalance = true;
                    }

                    _db.Customers.Update(member);

                    if (updateSessionIfCurrentUser)
                    {
                        var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!string.IsNullOrEmpty(claimId) &&
                            string.Equals(claimId, member.CustomerId, StringComparison.OrdinalIgnoreCase))
                        {
                            HttpContext.Session.SetString("CustomerId", member.CustomerId);
                            HttpContext.Session.SetString("CustomerPoints", member.CustomerRewardPoints.ToString());
                        }
                    }
                }
            }

            // Clear snapshot so we don’t double-rollback later
            order.PaymentCompletedAt = null;
            order.RewardPointsAwardedAt = null;
            order.RewardPointsRedeemed = 0;
            order.RewardPointsEarned = 0;
            order.TotalDiscountAmount = 0m;
            order.NetPayableAmount = 0m;

            _db.CustomerOrders.Update(order);
            await _db.SaveChangesAsync();

            return touchedBalance;
        }




        // =========================================================
        // Customer-facing: My CustomerOrders
        // =========================================================

        // GET: /Order/MyOrders
        [HttpGet]
        [Authorize] // require login to see personal order history (remove if you want guests too)
        public async Task<IActionResult> MyOrders()
        {
            var customerId = await EnsureCustomerIdAsync();

            var orders = await _db.CustomerOrders
                .Include(o => o.CustomerOrderDetails).ThenInclude(od => od.MenuItem)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderCreatedAt)
                .ToListAsync();

            return View(orders); // Create Views/Order/MyOrders.cshtml
        }

        private string? GetOrderRecipientEmail(CustomerOrder order)
        {
            // 1) Member email (if not guest)
            if (!string.IsNullOrWhiteSpace(order.CustomerId) && !order.CustomerId.StartsWith("G"))
            {
                var member = _db.Customers.AsNoTracking()
                    .FirstOrDefault(c => c.CustomerId == order.CustomerId);
                if (!string.IsNullOrWhiteSpace(member?.CustomerEmailAddress)) return member!.CustomerEmailAddress;
            }

            // 2) Logged-in claim fallback
            var claimEmail = User?.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrWhiteSpace(claimEmail)) return claimEmail;

            // 3) No email available (guest)
            return null;
        }

        private string BuildReceiptHtml(CustomerOrder order)
        {
            var lines = order.CustomerOrderDetails ?? new List<OrderDetail>();
            var subtotal = order.OrderTotalAmount;
            var discount = order.TotalDiscountAmount;
            var total = order.NetPayableAmount > 0 ? order.NetPayableAmount : (subtotal - discount);
            if (total < 0) total = 0;

            string Safe(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");

            string ItemRow(OrderDetail od)
            {
                var name = Safe(od.MenuItem?.MenuItemName ?? "Item");
                var basePrice = od.MenuItem?.MenuItemUnitPrice ?? 0m;
                var custom = od.AppliedCustomizations?.Sum(c => c.CustomizationAdditionalPrice) ?? 0m;
                var unit = basePrice + custom;
                var lineTotal = unit * od.OrderedQuantity;

                var customText = (od.AppliedCustomizations != null && od.AppliedCustomizations.Any())
                    ? $"<div style='color:#6b7280;font-size:12px;line-height:18px;margin-top:2px;'>+ {Safe(string.Join(", ", od.AppliedCustomizations.Select(c => c.CustomizationName)))}</div>"
                    : "";

                return $@"
<tr>
  <td style='padding:12px 0; border-bottom:1px solid #e5e7eb;'>
    <div style='font-weight:600;color:#111827;'>{name}</div>
    {customText}
  </td>
  <td style='padding:12px 0; border-bottom:1px solid #e5e7eb; text-align:center; color:#111827;'>{od.OrderedQuantity}</td>
  <td style='padding:12px 0; border-bottom:1px solid #e5e7eb; text-align:right; color:#111827;'>{unit.ToString("C")}</td>
  <td style='padding:12px 0; border-bottom:1px solid #e5e7eb; text-align:right; color:#111827; font-weight:600;'>{lineTotal.ToString("C")}</td>
</tr>";
            }

            var itemsHtml = string.Join("", lines.Select(ItemRow));
            var discountDisplay = discount > 0 ? ("- " + discount.ToString("C")) : (0m).ToString("C");

            return $@"
<!doctype html>
<html>
  <body style='margin:0; padding:0; background:#f3f4f6;'>
    <table role='presentation' cellpadding='0' cellspacing='0' border='0' align='center' width='100%' style='background:#f3f4f6; padding:24px 0;'>
      <tr>
        <td>
          <table role='presentation' cellpadding='0' cellspacing='0' border='0' align='center' width='600'
                 style='width:600px; max-width:100%; background:#ffffff; border:1px solid #e5e7eb; border-radius:12px; overflow:hidden; font-family:Inter,Segoe UI,Arial,sans-serif; color:#111827;'>

            <!-- Header -->
            <tr>
              <td style='padding:20px 24px; border-bottom:1px solid #e5e7eb;'>
                <div style='font-size:18px; font-weight:800;'>Snömi Café</div>
                <div style='font-size:12px; color:#6b7280; margin-top:2px;'>Receipt &nbsp;•&nbsp; {order.OrderCreatedAt:g}</div>
              </td>
            </tr>

            <!-- Title -->
            <tr>
              <td style='padding:20px 24px 4px 24px;'>
                <div style='font-size:16px; font-weight:700; margin-bottom:4px;'>Thanks for your order!</div>
                <div style='font-size:12px; color:#6b7280;'>Order ID: <span style='font-weight:700; color:#111827;'>{Safe(order.CustomerOrderId)}</span></div>
              </td>
            </tr>

            <!-- Order Meta -->
            <tr>
              <td style='padding:0 24px 16px 24px;'>
                <table width='100%' cellpadding='0' cellspacing='0' style='border-collapse:collapse;'>
                  <tr>
                    <td style='padding:6px 0; color:#374151;'>Payment</td>
                    <td style='padding:6px 0; text-align:right; color:#111827; font-weight:600;'>{Safe(order.PaymentMethodName ?? "N/A")}</td>
                  </tr>
                  <tr>
                    <td style='padding:6px 0; color:#374151;'>Dining</td>
                    <td style='padding:6px 0; text-align:right; color:#111827; font-weight:600;'>{(order.TableNumber > 0 ? "Table " + order.TableNumber : "Takeaway")}</td>
                  </tr>
                </table>
              </td>
            </tr>

            <!-- Items Table -->
            <tr>
              <td style='padding:0 24px 8px 24px;'>
                <table width='100%' cellpadding='0' cellspacing='0' style='border-collapse:collapse;'>
                  <thead>
                    <tr>
                      <th align='left'   style='padding:8px 0; color:#6b7280; font-size:12px; font-weight:600; border-bottom:1px solid #e5e7eb;'>Item</th>
                      <th align='center' style='padding:8px 0; color:#6b7280; font-size:12px; font-weight:600; border-bottom:1px solid #e5e7eb;'>Qty</th>
                      <th align='right'  style='padding:8px 0; color:#6b7280; font-size:12px; font-weight:600; border-bottom:1px solid #e5e7eb;'>MenuItemUnitPrice</th>
                      <th align='right'  style='padding:8px 0; color:#6b7280; font-size:12px; font-weight:600; border-bottom:1px solid #e5e7eb;'>OrderTotalAmount</th>
                    </tr>
                  </thead>
                  <tbody>
                    {itemsHtml}
                  </tbody>
                </table>
              </td>
            </tr>

            <!-- Totals Card -->
            <tr>
              <td style='padding:12px 24px 20px 24px;'>
                <table width='100%' cellpadding='0' cellspacing='0' role='presentation'
                       style='border:1px solid #e5e7eb; border-radius:10px; background:#f9fafb; padding:12px 16px;'>
                  <tr>
                    <td style='padding:6px 0; color:#374151;'>Subtotal</td>
                    <td style='padding:6px 0; text-align:right; color:#111827; font-weight:600;'>{subtotal.ToString("C")}</td>
                  </tr>
                  <tr>
                    <td style='padding:6px 0; color:#374151;'>Discount (points)</td>
                    <td style='padding:6px 0; text-align:right; color:#059669; font-weight:700;'>{discountDisplay}</td>
                  </tr>
                  <tr>
                    <td colspan='2' style='padding:8px 0;'><div style='height:1px; background:#e5e7eb;'></div></td>
                  </tr>
                  <tr>
                    <td style='padding:6px 0; font-weight:800; color:#111827;'>Total Paid</td>
                    <td style='padding:6px 0; text-align:right; font-size:18px; font-weight:800; color:#111827;'>{total.ToString("C")}</td>
                  </tr>
                  {(order.PaymentCompletedAt.HasValue
                              ? $"<tr><td colspan='2' style='padding-top:4px; color:#6b7280; font-size:12px;'>Paid on {order.PaymentCompletedAt.Value:g}</td></tr>"
                              : "")}
                </table>
              </td>
            </tr>

            <!-- Footer -->
            <tr>
              <td style='padding:16px 24px 22px 24px; border-top:1px solid #e5e7eb;'>
                <div style='font-size:12px; color:#6b7280; line-height:18px;'>
                  Snömi Café • This email is your tax receipt/invoice.<br/>
                  Questions? Reply to this email and we’ll help.
                </div>
              </td>
            </tr>

          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";
        }




        private async Task SendReceiptIfPossibleAsync(CustomerOrder order)
        {
            // send once
            if (order.EmailReceiptSentAt != null) return;

            var to = GetOrderRecipientEmail(order);
            if (string.IsNullOrWhiteSpace(to)) return;

            var subject = $"Receipt • {order.CustomerOrderId} • Snömi Café";

            // REMOVE any trackUrl code like:
            // var trackUrl = Url.Action("Track", "Order", new { id = order.CustomerOrderId }, Request.Scheme);

            // Call the new 1-parameter version
            var html = BuildReceiptHtml(order);

            using var mail = new MailMessage
            {
                Subject = subject,
                Body = html,
                IsBodyHtml = true
            };
            mail.To.Add(to);

            try
            {
                EmailHelper.SendEmail(mail, _cfg);
                order.EmailReceiptSentAt = DateTime.UtcNow;
                _db.CustomerOrders.Update(order);
                await _db.SaveChangesAsync();

                // Optional: surface a toast/alert after redirect
                TempData["Info"] = $"Receipt emailed to {to}.";
            }
            catch
            {

            }
        }



    }
}
*/