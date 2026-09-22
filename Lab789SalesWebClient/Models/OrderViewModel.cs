using System.ComponentModel.DataAnnotations;

namespace Lab789SalesWebClient.Models
{
    public class OrderViewModel
    {
        [Range(1,int.MaxValue, ErrorMessage = "Please select a product.")]
        public int ProductId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Range(1, 100000, ErrorMessage ="Quantity must be a positive number.")]
        public int Quantity { get; set; }
    }
}
