using Lab789HRWebClient.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Lab789HRWebClient.Controllers
{
    [Authorize]
    public class TransferRequestsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _hrApiUrl;
        private readonly string _authServerUrl;

        public TransferRequestsController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _hrApiUrl = configuration["ServiceUrls:HRWebAPI"] ?? "http://localhost:5146";
            _authServerUrl = configuration["ServiceUrls:AuthServer"] ?? "http://localhost:5264";
        }

        private async Task<HttpClient> CreateAuthorizedClientAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var currentUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                                   ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                                   ?? User.Identity?.Name;

            string? token = null;

            if (!string.IsNullOrEmpty(currentUserEmail))
            {
                // Verify if token in session matches currently logged in user
                var sessionUser = HttpContext.Session.GetString("JWT_TOKEN_USER");
                if (string.Equals(sessionUser, currentUserEmail, StringComparison.OrdinalIgnoreCase))
                {
                    token = HttpContext.Session.GetString("JWT_TOKEN");
                }
                else
                {
                    // Token in session belongs to previous user (e.g. manager logged in earlier) -> invalidate
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

        // 1. Employee views their own requests
        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            var client = await CreateAuthorizedClientAsync();
            var response = await client.GetAsync($"{_hrApiUrl}/api/TransferRequests/my-requests");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Unable to load your transfer requests";
                return View(new List<DepartmentTransferRequestViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var requests = JsonSerializer.Deserialize<List<DepartmentTransferRequestViewModel>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(requests ?? new List<DepartmentTransferRequestViewModel>());
        }

        // 2. Employee opens transfer request form
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CreateTransferRequestViewModel();
            await LoadCurrentEmployeeInfoAsync(model);
            return View(model);
        }

        private async Task LoadCurrentEmployeeInfoAsync(CreateTransferRequestViewModel model)
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? User.Identity?.Name;

            if (!string.IsNullOrEmpty(userEmail))
            {
                model.CurrentEmail = userEmail;
                var client = await CreateAuthorizedClientAsync();
                var resp = await client.GetAsync($"{_hrApiUrl}/api/Employees/by-email/{Uri.EscapeDataString(userEmail)}");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var emp = JsonSerializer.Deserialize<Employee>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (emp != null)
                    {
                        model.CurrentFullName = emp.FullName;
                        model.CurrentEmail = emp.Email;
                        model.CurrentDepartment = emp.Department;
                        model.CurrentPosition = emp.Position;
                    }
                }
            }
        }

        // 3. Employee submits transfer request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTransferRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(model.CurrentDepartment))
                {
                    await LoadCurrentEmployeeInfoAsync(model);
                }
                return View(model);
            }

            var client = await CreateAuthorizedClientAsync();
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(new { 
                    toDepartment = model.ToDepartment, 
                    toPosition = model.ToPosition, 
                    reason = model.Reason 
                }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync($"{_hrApiUrl}/api/TransferRequests", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Department transfer request submitted successfully! Waiting for manager approval.";
                return RedirectToAction(nameof(MyRequests));
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(errorContent);
                if (doc.RootElement.TryGetProperty("message", out var msgElement))
                {
                    ModelState.AddModelError("", msgElement.GetString() ?? "Error submitting transfer request");
                }
                else
                {
                    ModelState.AddModelError("", "Error submitting transfer request: " + errorContent);
                }
            }
            catch
            {
                ModelState.AddModelError("", "An error occurred while submitting the transfer request");
            }

            if (string.IsNullOrEmpty(model.CurrentDepartment))
            {
                await LoadCurrentEmployeeInfoAsync(model);
            }

            return View(model);
        }

        // 4. Manager/Admin views pending requests
        [HttpGet]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> Pending()
        {
            var client = await CreateAuthorizedClientAsync();
            var response = await client.GetAsync($"{_hrApiUrl}/api/TransferRequests/pending");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Unable to load pending transfer requests";
                return View(new List<DepartmentTransferRequestViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var requests = JsonSerializer.Deserialize<List<DepartmentTransferRequestViewModel>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(requests ?? new List<DepartmentTransferRequestViewModel>());
        }

        // 5. Manager/Admin reviews (approves or rejects) a request
        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int id, bool isApproved, string? reviewNote, string? approvedPosition = null)
        {
            var client = await CreateAuthorizedClientAsync();
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(new { isApproved, reviewNote, approvedPosition }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PutAsync($"{_hrApiUrl}/api/TransferRequests/{id}/review", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = isApproved
                    ? "Transfer request approved successfully! Employee department and position updated."
                    : "Transfer request rejected.";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = "Error processing transfer request: " + error;
            }

            return RedirectToAction(nameof(Pending));
        }
    }
}
