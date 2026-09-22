using Lab789SalesWebAPI.Core.Dtos;
using Lab789SalesWebAPI.Core.Entities;
using Lab789SalesWebAPI.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab789SalesWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly SaleDbContext _context;
        public OrdersController(SaleDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _context.Orders.AsNoTracking().Include(o => o.Product).OrderByDescending(o => o.OrderDate).Select(o => new
            {
                o.Id,
                o.ProductId,
                ProductName = o.Product.ProductName,
                o.OrderDate,
                o.Quantity,
                o.Product.Price,
                Amount = o.Product.Price * o.Quantity
            }).ToListAsync();
            return Ok(orders);
        }

        [HttpPost]
        public async Task<IActionResult> Create(OrderDto dto)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId);
            if (product == null)
            {
                return NotFound(new { message = "Product not found." });
            }
            if (dto.Quantity > product.Quantity)
            {
                return BadRequest(new { message = "Insufficient product quantity." });
            }
            var order = new Order
            {
                ProductId = dto.ProductId,
                OrderDate = dto.OrderDate == default ? DateTime.Now : dto.OrderDate,
                Quantity = dto.Quantity
            };
            product.Quantity -= dto.Quantity;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return Ok(new 
            {
                order.Id,
                order.ProductId,
                ProductName = product.ProductName,
                product.Price,
                order.Quantity,
                order.OrderDate,
                Amount = product.Price * order.Quantity
            });
        }   
    }
}
