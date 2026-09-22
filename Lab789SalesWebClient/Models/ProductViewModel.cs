using System.ComponentModel.DataAnnotations;

namespace Lab789SalesWebClient.Models
{
    public class ProductViewModel
    {
        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Range(1,1000000)]
        public decimal Price { get; set; }

        [Range(1, 100000)]
        public int Quantity { get; set; }

        public IFormFile? Image { get; set; }
    }
}
