using BookStoreWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace BookStoreWeb.Data
{
    public class BookStoreContext : DbContext
    {
        public BookStoreContext(DbContextOptions<BookStoreContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Publisher> Publishers { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Dữ liệu mẫu khởi tạo khi tạo CSDL
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, CategoryName = "Công nghệ thông tin", Description = "Sách lập trình, AI" },
                new Category { CategoryId = 2, CategoryName = "Kinh tế - Quản trị", Description = "Sách khởi nghiệp, kinh doanh" },
                new Category { CategoryId = 3, CategoryName = "Văn học", Description = "Tiểu thuyết, truyện ngắn" }
            );

            modelBuilder.Entity<Author>().HasData(
                new Author { AuthorId = 1, AuthorName = "Robert C. Martin", Bio = "Chuyên gia phần mềm" },
                new Author { AuthorId = 2, AuthorName = "Nguyễn Nhật Ánh", Bio = "Nhà văn nổi tiếng" }
            );

            modelBuilder.Entity<Publisher>().HasData(
                new Publisher { PublisherId = 1, PublisherName = "NXB Trẻ", Address = "TP.HCM" },
                new Publisher { PublisherId = 2, PublisherName = "NXB Thông tin và Truyền thông", Address = "Hà Nội" }
            );

            modelBuilder.Entity<AppUser>().HasData(
                new AppUser { UserId = 1, Username = "admin", PasswordHash = "123456", FullName = "Quản trị viên", Email = "admin@bookstore.vn", Role = "Admin" }
            );

            modelBuilder.Entity<Book>().HasData(
                new Book { BookId = 1, Title = "Clean Code", Price = 285000, StockQuantity = 50, PublishYear = 2024, CategoryId = 1, AuthorId = 1, PublisherId = 2, Description = "Sách viết code sạch." },
                new Book { BookId = 2, Title = "Mắt Biếc", Price = 110000, StockQuantity = 100, PublishYear = 2023, CategoryId = 3, AuthorId = 2, PublisherId = 1, Description = "Truyện Nguyễn Nhật Ánh." }
            );
        }
    }
}