using System.ComponentModel.DataAnnotations;

namespace BookStoreWeb.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tên thể loại không được để trống")]
        [StringLength(100)]
        [Display(Name = "Tên thể loại")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        public virtual ICollection<Book>? Books { get; set; }
    }
}