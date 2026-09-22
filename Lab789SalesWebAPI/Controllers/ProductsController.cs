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
    public class ProductsController : ControllerBase
    {
        private readonly SaleDbContext _context;
        public ProductsController(SaleDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAll()
        {
            var products = await _context.Products.AsNoTracking().OrderByDescending(p => p.Id).ToListAsync();
            return Ok(products);
        }

        [HttpPost]
        public async Task<ActionResult<Product>> Create(ProductDto dto)
        {
            string? imageUrl = null;
            if (dto.Image != null)
            {
                var extension = Path.GetExtension(dto.Image.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new
                    {
                        message = "Invalid image format. Only JPG, JPEG, PNG, and GIF are allowed."
                    });
                }
                if (dto.Image.Length > 2 * 1024 * 1024)
                {
                    return BadRequest(new
                    {
                        message = "Image size exceeds the limit of 2MB."
                    });
                }
                var fileName = $"{Guid.NewGuid()}{extension}";
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                Directory.CreateDirectory(folder);
                var filePath = Path.Combine(folder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await dto.Image.CopyToAsync(stream);
                imageUrl = $"/images/{fileName}";
            }
            var product = new Product
            {
                ProductName = dto.ProductName.Trim(),
                Category = dto.Category.Trim(),
                Price = dto.Price,
                Quantity = dto.Quantity,
                ImageURL = imageUrl
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Ok(product);
        }
    }
}
