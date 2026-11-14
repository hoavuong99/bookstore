using BookStoreWeb.Data;
using BookStoreWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStoreWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly BookStoreContext _context;

        public HomeController(BookStoreContext context)
        {
            _context = context;
        }

        // GET: /Home/Index (Trang chủ + Lọc + Phân trang + Top Sách Bán Chạy)
        public async Task<IActionResult> Index(int? categoryId, string searchString, int page = 1)
        {
            int pageSize = 6;
            var booksQuery = _context.Books
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                booksQuery = booksQuery.Where(b => b.CategoryId == categoryId.Value);
                ViewBag.SelectedCategory = categoryId.Value;
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                booksQuery = booksQuery.Where(b => b.Title.Contains(searchString));
                ViewBag.SearchString = searchString;
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            // Lấy trực tiếp dữ liệu chữ chạy & banner từ PromoManager do Admin cài đặt
            var promo = PromoManager.Load();
            if (promo != null)
            {
                ViewBag.IsPromoActive = promo.IsActive;             // Trạng thái bật/tắt từ nút Kích hoạt trong Admin
                ViewBag.PromoTitle = promo.ProgramTitle;            // Tiêu đề chương trình
                ViewBag.PromoMarquee = promo.MarqueeText;           // Nội dung chữ chạy Marquee
            }
            else
            {
                ViewBag.IsPromoActive = false;
            }
            // Thống kê số lượng đã bán của từng sách từ bảng OrderDetails để xếp hạng Sách Bán Chạy
            var salesDict = await _context.OrderDetails
                .GroupBy(od => od.BookId)
                .Select(g => new { BookId = g.Key, TotalSold = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.BookId, x => x.TotalSold);

            var allBooks = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .ToListAsync();

            // Lấy Top 4 cuốn sách bán chạy nhất / hot nhất đưa ra cột bên trái
            var hotBooks = allBooks
                .OrderByDescending(b => salesDict.ContainsKey(b.BookId) ? salesDict[b.BookId] : 0)
                .ThenByDescending(b => PromoManager.GetEffectiveDiscount(b.BookId, b.CategoryId))
                .ThenByDescending(b => b.BookId)
                .Take(4)
                .ToList();

            ViewBag.HotBooks = hotBooks;
            ViewBag.SalesDict = salesDict;

            // Phân trang
            int totalBooks = await booksQuery.CountAsync();
            var books = await booksQuery
                .OrderByDescending(b => b.BookId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalBooks / (double)pageSize);

            return View(books);
        }

        // Xử lý khi khách hàng gửi yêu cầu hỗ trợ / thắc mắc từ Tổng đài
        [HttpPost]
        public IActionResult SendSupport(string fullName, string phone, string topic, string message)
        {
            if (!string.IsNullOrWhiteSpace(fullName) && !string.IsNullOrWhiteSpace(phone))
            {
                var tickets = PromoManager.LoadTickets();
                int nextId = tickets.Any() ? tickets.Max(t => t.Id) + 1 : 1;

                tickets.Insert(0, new SupportTicket
                {
                    Id = nextId,
                    FullName = fullName.Trim(),
                    Phone = phone.Trim(),
                    Topic = topic ?? "Tư vấn mua sách",
                    Message = message?.Trim() ?? "",
                    CreatedAt = DateTime.Now,
                    IsResolved = false
                });

                PromoManager.SaveTickets(tickets);
                TempData["SuccessMessage"] = $"🎧 Đã gửi yêu cầu hỗ trợ thành công! Tổng đài viên sẽ liên hệ SĐT {phone} của bạn trong ít phút.";
            }
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}