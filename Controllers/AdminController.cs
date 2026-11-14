using BookStoreWeb.Data;
using BookStoreWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStoreWeb.Controllers
{
    public class AdminController : Controller
    {
        private readonly BookStoreContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(BookStoreContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin";

        // 1. TRANG CHỦ ADMIN: BÁO CÁO THỐNG KÊ, DUYỆT ĐƠN, QUẢN LÝ USER & TIN NHẮN HỖ TRỢ
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            ViewBag.TotalBooks = _context.Books.Count();
            ViewBag.TotalStock = _context.Books.Sum(b => (int?)b.StockQuantity) ?? 0;
            ViewBag.TotalOrders = _context.Orders.Count();

            // Tổng doanh thu (không tính các đơn đã hủy)
            ViewBag.TotalRevenue = _context.Orders
                .Where(o => !o.Status.Contains("Đã hủy"))
                .Sum(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.Users = _context.AppUsers.OrderByDescending(u => u.UserId).ToList();
            ViewBag.Tickets = PromoManager.LoadTickets(); // Lấy danh sách tin nhắn/yêu cầu hỗ trợ từ khách

            // 👉 BỔ SUNG: Lấy riêng danh sách đơn hàng đã hủy để phục vụ báo cáo lý do hủy
            ViewBag.CancelledOrders = _context.Orders
                .Where(o => o.Status.Contains("Đã hủy"))
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            var orders = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book!)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // Đánh dấu đã xử lý tin nhắn / yêu cầu hỗ trợ của khách
        [HttpPost]
        public IActionResult ResolveTicket(int ticketId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            var tickets = PromoManager.LoadTickets();
            var ticket = tickets.FirstOrDefault(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.IsResolved = true;
                PromoManager.SaveTickets(tickets);
                TempData["SuccessMessage"] = $"Đã xác nhận xử lý yêu cầu hỗ trợ từ khách hàng {ticket.FullName}!";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateOrderStatus(int orderId, string status, string? cancelReason)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            var order = _context.Orders.Find(orderId);
            if (order != null)
            {
                order.Status = status;
                if (status == "Đã hủy" && !string.IsNullOrWhiteSpace(cancelReason))
                {
                    order.ShippingAddress += $" [LÝ DO HỦY: {cancelReason.Trim()}]";
                }
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng #{orderId} thành: {status}" + (status == "Đã hủy" ? $" (Lý do: {cancelReason})" : "");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ToggleUserRole(int userId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            var user = _context.AppUsers.Find(userId);
            if (user != null && user.Username != "admin")
            {
                user.Role = (user.Role == "Admin") ? "Customer" : "Admin";
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Đã đổi quyền tài khoản {user.Username} thành {user.Role}!";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteUser(int userId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            var user = _context.AppUsers.Find(userId);
            if (user != null && user.Username != "admin")
            {
                _context.AppUsers.Remove(user);
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Đã xóa tài khoản {user.Username}!";
            }
            return RedirectToAction("Index");
        }

        // 2. QUẢN LÝ KHO SÁCH, KHUYẾN MÃI & DANH MỤC
        public IActionResult ManageBooks()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Authors = _context.Authors.ToList();
            ViewBag.Publishers = _context.Publishers.ToList();
            ViewBag.Promo = PromoManager.Load();

            var books = _context.Books
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .OrderByDescending(b => b.BookId)
                .ToList();

            return View(books);
        }

        [HttpPost]
        public IActionResult UpdatePromotionBanner(string programTitle, string marqueeText, bool isActive)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            var promo = PromoManager.Load();
            promo.ProgramTitle = programTitle ?? "";
            promo.MarqueeText = marqueeText ?? "";
            promo.IsActive = isActive;
            PromoManager.Save(promo);

            TempData["SuccessMessage"] = "Đã cập nhật chương trình quảng cáo & chữ chạy khuyến mãi!";
            return RedirectToAction("ManageBooks");
        }

        [HttpPost]
        public IActionResult AddCategory(string categoryName, int discountPercent = 0)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var cat = new Category { CategoryName = categoryName.Trim() };
                _context.Categories.Add(cat);
                _context.SaveChanges();

                if (discountPercent > 0)
                {
                    var promo = PromoManager.Load();
                    promo.CategoryDiscounts[cat.CategoryId] = Math.Clamp(discountPercent, 0, 95);
                    PromoManager.Save(promo);
                }
                TempData["SuccessMessage"] = $"Đã thêm thể loại: {categoryName} (Giảm {discountPercent}%)";
            }
            return RedirectToAction("ManageBooks");
        }

        [HttpPost]
        public IActionResult EditCategory(int categoryId, string categoryName, int discountPercent)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            var cat = _context.Categories.Find(categoryId);
            if (cat != null && !string.IsNullOrWhiteSpace(categoryName))
            {
                cat.CategoryName = categoryName.Trim();
                _context.SaveChanges();

                var promo = PromoManager.Load();
                promo.CategoryDiscounts[categoryId] = Math.Clamp(discountPercent, 0, 95);
                PromoManager.Save(promo);

                TempData["SuccessMessage"] = $"Đã cập nhật thể loại '{cat.CategoryName}' - Khuyến mãi loại này: {discountPercent}%!";
            }
            return RedirectToAction("ManageBooks");
        }

        [HttpPost]
        public IActionResult DeleteCategory(int categoryId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (_context.Books.Any(b => b.CategoryId == categoryId))
            {
                TempData["SuccessMessage"] = "⚠️ Không thể xóa thể loại đang có sách! Bạn có thể SỬA tên thể loại trực tiếp ngay trên dòng đó.";
                return RedirectToAction("ManageBooks");
            }
            var cat = _context.Categories.Find(categoryId);
            if (cat != null)
            {
                _context.Categories.Remove(cat);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa thể loại thành công!";
            }
            return RedirectToAction("ManageBooks");
        }

        [HttpPost]
        public IActionResult AddAuthor(string authorName)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (!string.IsNullOrWhiteSpace(authorName))
            {
                _context.Authors.Add(new Author { AuthorName = authorName.Trim() });
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Đã thêm tác giả: {authorName}";
            }
            return RedirectToAction("ManageBooks");
        }

        [HttpPost]
        public IActionResult EditAuthor(int authorId, string authorName)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            var author = _context.Authors.Find(authorId);
            if (author != null && !string.IsNullOrWhiteSpace(authorName))
            {
                author.AuthorName = authorName.Trim();
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Đã sửa tên tác giả thành: {author.AuthorName}";
            }
            return RedirectToAction("ManageBooks");
        }

        [HttpPost]
        public IActionResult DeleteAuthor(int authorId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (_context.Books.Any(b => b.AuthorId == authorId))
            {
                TempData["SuccessMessage"] = "⚠️ Không thể xóa tác giả đang có sách!";
                return RedirectToAction("ManageBooks");
            }
            var author = _context.Authors.Find(authorId);
            if (author != null)
            {
                _context.Authors.Remove(author);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa tác giả!";
            }
            return RedirectToAction("ManageBooks");
        }

        [HttpPost]
        public IActionResult AddPublisher(string publisherName)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (!string.IsNullOrWhiteSpace(publisherName))
            {
                _context.Publishers.Add(new Publisher { PublisherName = publisherName.Trim() });
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Đã thêm NXB: {publisherName}";
            }
            return RedirectToAction("ManageBooks");
        }

        [HttpPost]
        public IActionResult EditPublisher(int publisherId, string publisherName)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            var pub = _context.Publishers.Find(publisherId);
            if (pub != null && !string.IsNullOrWhiteSpace(publisherName))
            {
                pub.PublisherName = publisherName.Trim();
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Đã sửa tên NXB thành: {pub.PublisherName}";
            }
            return RedirectToAction("ManageBooks");
        }

        [HttpPost]
        public IActionResult DeletePublisher(int publisherId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (_context.Books.Any(b => b.PublisherId == publisherId))
            {
                TempData["SuccessMessage"] = "⚠️ Không thể xóa NXB đang có sách!";
                return RedirectToAction("ManageBooks");
            }
            var pub = _context.Publishers.Find(publisherId);
            if (pub != null)
            {
                _context.Publishers.Remove(pub);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa NXB!";
            }
            return RedirectToAction("ManageBooks");
        }

        [HttpPost]
        public IActionResult UpdateStock(int bookId, int categoryId, int authorId, int publisherId, decimal price, int discountPercent, int stockQuantity)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            var book = _context.Books.Find(bookId);
            if (book != null)
            {
                book.CategoryId = categoryId;
                book.AuthorId = authorId;
                book.PublisherId = publisherId;
                book.Price = price;
                book.StockQuantity = stockQuantity;
                _context.SaveChanges();

                var promo = PromoManager.Load();
                promo.BookDiscounts[bookId] = Math.Clamp(discountPercent, 0, 95);
                PromoManager.Save(promo);

                TempData["SuccessMessage"] = $"Đã lưu cập nhật cho sách '{book.Title}'!";
            }
            return RedirectToAction("ManageBooks");
        }

        public IActionResult CreateBook(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Authors = _context.Authors.ToList();
            ViewBag.Publishers = _context.Publishers.ToList();

            var promo = PromoManager.Load();
            ViewBag.BookDiscount = 0;

            if (id.HasValue && id.Value > 0)
            {
                var existingBook = _context.Books.Find(id.Value);
                if (existingBook != null)
                {
                    ViewBag.BookDiscount = promo.BookDiscounts.TryGetValue(existingBook.BookId, out int d) ? d : 0;
                    return View(existingBook);
                }
            }

            return View(new Book { PublishYear = DateTime.Now.Year, StockQuantity = 20 });
        }

        [HttpPost]
        public async Task<IActionResult> CreateBook(Book book, int discountPercent = 0, IFormFile? imageFile = null)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            ModelState.Remove("Category");
            ModelState.Remove("Author");
            ModelState.Remove("Publisher");
            ModelState.Remove("OrderDetails");
            ModelState.Remove("imageFile");

            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "books");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string ext = Path.GetExtension(imageFile.FileName);
                string uniqueFileName = Guid.NewGuid().ToString("N").Substring(0, 8) + "_" + Path.GetFileNameWithoutExtension(imageFile.FileName) + ext;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                book.CoverImage = "/images/books/" + uniqueFileName;
            }

            if (book.BookId == 0)
            {
                _context.Books.Add(book);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã thêm sách mới '{book.Title}' thành công!";
            }
            else
            {
                var existing = _context.Books.AsNoTracking().FirstOrDefault(b => b.BookId == book.BookId);
                if (existing != null && string.IsNullOrEmpty(book.CoverImage))
                {
                    book.CoverImage = existing.CoverImage;
                }
                _context.Books.Update(book);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật thông tin sách '{book.Title}' thành công!";
            }

            var promo = PromoManager.Load();
            promo.BookDiscounts[book.BookId] = Math.Clamp(discountPercent, 0, 95);
            PromoManager.Save(promo);

            return RedirectToAction("ManageBooks");
        }

        public IActionResult DeleteBook(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            var book = _context.Books.Find(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa sách khỏi hệ thống!";
            }
            return RedirectToAction("ManageBooks");
        }
        [HttpPost]
        public IActionResult SendNotification(string title, string content)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(content))
            {
                var notifs = PromoManager.LoadNotifications();
                int newId = notifs.Any() ? notifs.Max(n => n.Id) + 1 : 1;

                notifs.Insert(0, new NotificationMessage
                {
                    Id = newId,
                    Title = title.Trim(),
                    Content = content.Trim(),
                    CreatedAt = DateTime.Now
                });

                PromoManager.SaveNotifications(notifs);
                TempData["SuccessMessage"] = "📢 Đã gửi thông báo khuyến mãi thành công đến toàn bộ thành viên!";
            }
            return RedirectToAction("Index");
        }
    }
}