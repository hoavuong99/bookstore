using BookStoreWeb.Data;
using BookStoreWeb.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BookStoreWeb.Controllers
{
    public class CartController : Controller
    {
        private readonly BookStoreContext _context;
        private const string CART_KEY = "MY_CART";

        public CartController(BookStoreContext context)
        {
            _context = context;
        }

        private List<CartItem> GetCart()
        {
            var session = HttpContext.Session;
            string? jsonCart = session.GetString(CART_KEY);
            if (string.IsNullOrEmpty(jsonCart)) return new List<CartItem>();
            return JsonSerializer.Deserialize<List<CartItem>>(jsonCart) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            var session = HttpContext.Session;
            session.SetString(CART_KEY, JsonSerializer.Serialize(cart));
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            ViewBag.Total = cart.Sum(item => item.TotalPrice);
            return View(cart);
        }

        [HttpPost]
        public IActionResult AddToCart(int bookId, int quantity = 1)
        {
            var book = _context.Books.Find(bookId);
            if (book == null) return Json(new { success = false, message = "Không tìm thấy sách!" });

            int discount = PromoManager.GetEffectiveDiscount(book.BookId, book.CategoryId);
            decimal finalPrice = PromoManager.GetFinalPrice(book.Price, discount);

            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.BookId == bookId);
            if (item != null)
            {
                item.Quantity += quantity;
                item.OriginalPrice = book.Price;
                item.DiscountPercent = discount;
                item.Price = finalPrice;
            }
            else
            {
                cart.Add(new CartItem
                {
                    BookId = book.BookId,
                    Title = book.Title,
                    OriginalPrice = book.Price,
                    DiscountPercent = discount,
                    Price = finalPrice,
                    Quantity = quantity,
                    CoverImage = book.CoverImage
                });
            }

            SaveCart(cart);
            string msg = discount > 0
                ? $"Đã thêm '{book.Title}' vào giỏ (Giá ưu đãi -{discount}%: {finalPrice:N0}đ)!"
                : $"Đã thêm '{book.Title}' vào giỏ hàng!";

            return Json(new { success = true, totalItems = cart.Sum(c => c.Quantity), message = msg });
        }

        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.BookId == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        // HÀM CHECKOUT XỬ LÝ ĐẶT HÀNG VÀ THANH TOÁN (COD, CHUYỂN KHOẢN, VÍ ĐIỆN TỬ)
        [HttpPost]
        public IActionResult Checkout(string receiverName, string receiverPhone, string shippingAddress, string paymentMethod)
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");

            int? currentUserId = HttpContext.Session.GetInt32("UserId");

            var order = new Order
            {
                UserId = currentUserId,
                OrderDate = DateTime.Now,
                ReceiverName = receiverName,
                ReceiverPhone = receiverPhone,
                ShippingAddress = $"{shippingAddress} [Hình thức thanh toán: {paymentMethod ?? "Ship COD"}]",
                TotalAmount = cart.Sum(item => item.TotalPrice),
                Status = "Chờ duyệt",
                OrderDetails = cart.Select(item => new OrderDetail
                {
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                }).ToList()
            };

            foreach (var item in cart)
            {
                var book = _context.Books.Find(item.BookId);
                if (book != null && book.StockQuantity >= item.Quantity)
                {
                    book.StockQuantity -= item.Quantity;
                }
            }

            _context.Orders.Add(order);
            _context.SaveChanges();

            HttpContext.Session.Remove(CART_KEY);

            TempData["SuccessMessage"] = $"🎉 Đặt hàng thành công qua [{paymentMethod ?? "Ship COD"}]! Mã đơn hàng của bạn là #{order.OrderId}.";
            return RedirectToAction("MyOrders");
        }

        public IActionResult MyOrders()
        {
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            var query = _context.Orders.AsQueryable();

            if (currentUserId.HasValue && HttpContext.Session.GetString("Role") != "Admin")
            {
                query = query.Where(o => o.UserId == currentUserId.Value || o.UserId == null);
            }

            var orders = query.OrderByDescending(o => o.OrderId).ToList();
            return View(orders);
        }
    }
}