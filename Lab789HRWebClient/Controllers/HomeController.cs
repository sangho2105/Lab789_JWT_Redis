using Lab789HRWebClient.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Lab789HRWebClient.Controllers
{
    public class HomeController : Controller
    {
        private readonly string url = "http://localhost:5146/api/Employees";
        private readonly HttpClient httpClient;
        public HomeController(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
        
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var response = await this.httpClient.GetAsync(url);
            if(!response.IsSuccessStatusCode || User.Identity!.IsAuthenticated != true)
            {
                return View(new List<Employee>());
            }
            var json = await response.Content.ReadAsStringAsync();
            var employees = JsonSerializer.Deserialize<List<Employee>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return View(employees ?? new List<Employee>());
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task< IActionResult> Create(EmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8,"application/json");
            var response = await this.httpClient.PostAsync(url, content);   //bỏ 2 dòng trên thì sử dụng PostAsJsonAsync
            if(response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "Can't create employee");
            return View(model);
        }

       
    }
}
