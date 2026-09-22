using Lab789HRWebClient.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Lab789HRWebClient.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _hrApiUrl;
        private readonly string _authServerUrl;

        public HomeController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _hrApiUrl = configuration["ServiceUrls:HRWebAPI"] ?? "http://localhost:5146";
            _authServerUrl = configuration["ServiceUrls:AuthServer"] ?? "http://localhost:5264";
        }

        private async Task<HttpClient> CreateClientWithAuthAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var currentUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                                   ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                                   ?? User.Identity?.Name;

            string? token = null;

            if (!string.IsNullOrEmpty(currentUserEmail))
            {
                var sessionUser = HttpContext.Session.GetString("JWT_TOKEN_USER");
                if (string.Equals(sessionUser, currentUserEmail, StringComparison.OrdinalIgnoreCase))
                {
                    token = HttpContext.Session.GetString("JWT_TOKEN");
                }
                else
                {
                    HttpContext.Session.Remove("JWT_TOKEN");
                    HttpContext.Session.Remove("JWT_TOKEN_USER");
                }

                if (string.IsNullOrEmpty(token) && User.Identity?.IsAuthenticated == true)
                {
                    try
                    {
                        var authClient = _httpClientFactory.CreateClient();
                        var tokenResp = await authClient.GetAsync($"{_authServerUrl}/api/Auth/token-by-email?email={Uri.EscapeDataString(currentUserEmail)}");
                        if (tokenResp.IsSuccessStatusCode)
                        {
                            var json = await tokenResp.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(json);
                            if (doc.RootElement.TryGetProperty("token", out var tokenEl))
                            {
                                token = tokenEl.GetString();
                                if (!string.IsNullOrEmpty(token))
                                {
                                    HttpContext.Session.SetString("JWT_TOKEN", token);
                                    HttpContext.Session.SetString("JWT_TOKEN_USER", currentUserEmail);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }
        
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var client = await CreateClientWithAuthAsync();
            var response = await client.GetAsync($"{_hrApiUrl}/api/Employees");
            if (!response.IsSuccessStatusCode)
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

        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create() => View(new EmployeeViewModel());

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = await CreateClientWithAuthAsync();

            // 1. Create login account (Role: Employee) on AuthServer
            var regPayload = new
            {
                email = model.Email.Trim(),
                password = model.Password,
                role = "Employee"
            };
            var regContent = new StringContent(JsonSerializer.Serialize(regPayload), Encoding.UTF8, "application/json");
            var regResponse = await client.PostAsync($"{_authServerUrl}/api/Auth/register", regContent);

            if (!regResponse.IsSuccessStatusCode)
            {
                var regError = await regResponse.Content.ReadAsStringAsync();
                ModelState.AddModelError("", "Error creating user account on AuthServer: " + regError);
                return View(model);
            }

            // 2. Create employee record on HRWebAPI
            var empPayload = new
            {
                fullName = model.FullName.Trim(),
                email = model.Email.Trim(),
                department = model.Department.Trim(),
                position = model.Position.Trim(),
                salary = model.Salary
            };
            var empContent = new StringContent(JsonSerializer.Serialize(empPayload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_hrApiUrl}/api/Employees", empContent);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = $"Successfully created employee {model.FullName}! Account: {model.Email} (Password: {model.Password})";
                return RedirectToAction("Index");
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", "Failed to create employee profile: " + errorMsg);
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
