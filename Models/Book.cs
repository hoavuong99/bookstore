using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookStoreWeb.Models
{
    public class Book
    {
        [Key]
        public int BookId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sách")]
        [StringLength(200, MinimumLength = 2)]
        [Display(Name = "Tên sách")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá bán")]
        [Range(1000, 10000000, ErrorMessage = "Giá bán từ 1.000đ đến 10.000.000đ")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá bán")]
        public decimal Price { get; set; }
        [Display(Name = "Phần trăm giảm giá (%)")]
        [Range(0, 100, ErrorMessage = "Mức giảm giá từ 0% đến 100%")]
        public int Discount { get; set; } = 0;
        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn")]
        [Range(0, 10000)]
        [Display(Name = "Số lượng tồn")]
        public int StockQuantity { get; set; }

        [Display(Name = "Năm xuất bản")]
        public int PublishYear { get; set; } = 2026;

        [Display(Name = "Ảnh bìa")]
        public string? CoverImage { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thể loại")]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn tác giả")]
        public int AuthorId { get; set; }
        [ForeignKey("AuthorId")]
        public virtual Author? Author { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhà xuất bản")]
        public int PublisherId { get; set; }
        [ForeignKey("PublisherId")]
        public virtual Publisher? Publisher { get; set; }
    }
}