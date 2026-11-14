using System.ComponentModel.DataAnnotations;

namespace BookStoreWeb.Models
{
    public class AppUser
    {
        [Key]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Tài khoản không được để trống")]
        [StringLength(50, MinimumLength = 3)]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(255, MinimumLength = 6)]
        [Display(Name = "Mật khẩu")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Display(Name = "Vai trò")]
        public string Role { get; set; } = "Customer"; // "Admin" hoặc "Customer"
    }
}