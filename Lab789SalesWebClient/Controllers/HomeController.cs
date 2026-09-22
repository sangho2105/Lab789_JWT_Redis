using Lab789SalesWebClient.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Lab789SalesWebClient.Controllers
{
    public class HomeController : Controller
    {
        private readonly string productUrl = "http://localhost:5185/api/Products";
        private readonly string orderUrl = "http://localhost:5185/api/Orders";
        private readonly HttpClient httpClient;
        public HomeController(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<IActionResult> Index()
        {
            var response = await httpClient.GetAsync(this.productUrl);
            if(!response.IsSuccessStatusCode)
            {
                return View(new List<Product>());
            }
            var json = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<Product>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
            return View(products ?? new List<Product>());
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateProduct()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return View(model);
            }
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(model.ProductName),"ProductName");
            form.Add(new StringContent(model.Category), "Category");
            form.Add(new StringContent(model.Price.ToString(CultureInfo.InvariantCulture)), "Price");
            form.Add(new StringContent(model.Quantity.ToString()), "Quantity");
            if(model.Image != null)
            {
                var stream = model.Image.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(model.Image.ContentType);
                form.Add(fileContent, "Image", model.Image.FileName);
            }
            var response = await httpClient.PostAsync(productUrl, form);
            if(response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("", "Can't create product");
            return View(model);
        }

        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> Orders()
        {
            var response = await httpClient.GetAsync(this.orderUrl);
            if (!response.IsSuccessStatusCode)
            {
                return View(new List<Order>());
            }
            var json = await response.Content.ReadAsStringAsync();
            var orders = JsonSerializer.Deserialize<List<Order>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
            return View(orders ?? new List<Order>());
        }

        public async Task LoadProducts()
        {
            var response = await httpClient.GetAsync(productUrl);
            if(!response.IsSuccessStatusCode)
            {
                ViewBag.Products = new List<SelectListItem>();
                return;
            }
            var json = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<Product>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Product>();

            ViewBag.Products = products.Where(p => p.Quantity > 0).Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{ p.ProductName} - " + $"Price: {p.Price:N2} - " + $"Stock: {p.Quantity}"
            }).ToList();
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> CreateOrder()
        {
            var response = await httpClient.GetAsync(productUrl);
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Unable load products");
                return View(new OrderViewModel());
            }
            var json = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<Product>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Product>();

            ViewBag.Products = products.Where(p => p.Quantity > 0).Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.ProductName} - " + $"Price: {p.Price:N2} - " + $"Stock: {p.Quantity}"
            }).ToList();

            return View(new OrderViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Sales")]
        [ValidateAntiForgeryToken]  //XSS
        public async Task<IActionResult> CreateOrder(OrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadProducts();
                return View(model);
            }
            var response = await httpClient.PostAsJsonAsync(orderUrl, model);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Orders");
            }
            var errorMessage = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", string.IsNullOrEmpty( errorMessage) ? "Unable to create order" : errorMessage);
            await LoadProducts();
            return View(model);
        }
    }
}
