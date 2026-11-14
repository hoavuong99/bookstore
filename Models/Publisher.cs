using System.ComponentModel.DataAnnotations;

namespace BookStoreWeb.Models
{
    public class Publisher
    {
        [Key]
        public int PublisherId { get; set; }

        [Required(ErrorMessage = "Tên nhà xuất bản không được để trống")]
        [StringLength(150)]
        [Display(Name = "Nhà xuất bản")]
        public string PublisherName { get; set; } = string.Empty;

        [StringLength(250)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        public virtual ICollection<Book>? Books { get; set; }
    }
}