using BookStoreWeb.Data;
using BookStoreWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookStoreWeb.Controllers
{
    public class AuthController : Controller
    {
        private readonly BookStoreContext _context;

        public AuthController(BookStoreContext context)
        {
            _context = context;
        }

        // Trang Đăng nhập
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _context.AppUsers.FirstOrDefault(u => u.Username == username && u.PasswordHash == password);
            if (user != null)
            {
                // Lưu thông tin vào Session
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetInt32("UserId", user.UserId);

                TempData["SuccessMessage"] = "Đăng nhập thành công!";
                if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng!");
            return View();
        }

        // Đăng xuất
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đã đăng xuất hệ thống.";
            return RedirectToAction("Index", "Home");
        }
        // GET: /Auth/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Auth/ForgotPassword
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Vui lòng nhập địa chỉ email của bạn.";
                return View();
            }

            // Sử dụng _context.AppUsers khớp với database hiện tại của bạn
            var user = _context.AppUsers.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                TempData["Error"] = "Email này không tồn tại trong hệ thống.";
                return View();
            }

            return RedirectToAction("ResetPassword", new { email = email });

        }
        // GET: /Auth/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }
            ViewBag.Email = email;
            return View();
        }

        // POST: /Auth/ResetPassword
        [HttpPost]
        public IActionResult ResetPassword(string email, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Mật khẩu xác nhận không khớp!";
                ViewBag.Email = email;
                return View();
            }

            var user = _context.AppUsers.FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                // Cập nhật mật khẩu mới vào cơ sở dữ liệu
                user.PasswordHash = newPassword;
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại bằng mật khẩu mới.";
                return RedirectToAction("Login");
            }

            TempData["Error"] = "Không tìm thấy tài khoản tương ứng.";
            return View();
        }
        // Trang Đăng ký
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(AppUser user, string confirmPassword)
        {
            if (user.PasswordHash != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp!");
                return View(user);
            }

            if (ModelState.IsValid)
            {
                user.Role = "Customer";
                _context.AppUsers.Add(user);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            return View(user);
        }
    }
}