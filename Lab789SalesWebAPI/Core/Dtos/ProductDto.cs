using System.ComponentModel.DataAnnotations;

namespace Lab789SalesWebAPI.Core.Dtos
{
    public class ProductDto
    {
        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;
        
        [Range(1,1000000)]
        public decimal Price { get; set; }

        [Range(0, 100000)]
        public int Quantity { get; set; }


        public IFormFile? Image { get; set; }    //vì upload file nên phải dùng IFormFile, còn nếu chỉ lưu đường dẫn thì dùng string
    }
}
