using System.ComponentModel.DataAnnotations;

namespace Lab789SalesWebAPI.Core.Dtos
{
    public class OrderDto
    {
        [Range(1,int.MaxValue)]
        public int ProductId { get; set; }

        public DateTime OrderDate { get; set; }

        [Range(1,100000)]
        public int Quantity { get; set; }
    }
}
