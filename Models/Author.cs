using System.ComponentModel.DataAnnotations;

namespace BookStoreWeb.Models
{
    public class Author
    {
        [Key]
        public int AuthorId { get; set; }

        [Required(ErrorMessage = "Tên tác giả không được để trống")]
        [StringLength(100)]
        [Display(Name = "Tên tác giả")]
        public string AuthorName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Tiểu sử")]
        public string? Bio { get; set; }

        public virtual ICollection<Book>? Books { get; set; }
    }
}