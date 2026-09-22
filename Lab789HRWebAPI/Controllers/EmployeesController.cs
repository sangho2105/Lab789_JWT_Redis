using Lab789HRWebAPI.Core.Dtos;
using Lab789HRWebAPI.Core.Entities;
using Lab789HRWebAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Lab789HRWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly HRDBContext context;
        private readonly IDistributedCache _cache;
        private const string EmployeesCacheKey = "employees_list";
        public EmployeesController(HRDBContext context, IDistributedCache cache)
        {
            this.context = context;
            _cache = cache;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> GetAll()
        {
            // 1. Check Redis cache
            var cachedData = await _cache.GetStringAsync(EmployeesCacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedEmployees = JsonSerializer.Deserialize<List<Employee>>(cachedData);
                return Ok(cachedEmployees);
            }
            // 2. Cache Miss: Query Database
            var response = await context.Employees.AsNoTracking().OrderByDescending(e => e.Salary).ToListAsync();
            // 3. Write to Redis with 10-minute TTL
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            await _cache.SetStringAsync(EmployeesCacheKey, JsonSerializer.Serialize(response), options);
            return Ok(response);
        }
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<Employee>> Create(EmployeeDto dto)
        {
            var empExist = await context.Employees.AnyAsync(e => e.Email == dto.Email);
            if (empExist)
            {
                return Conflict(new { message = "Email already exists" });
            }
            var newEmployee = new Employee
            {
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim(),
                Department = dto.Department.Trim(),
                Position = dto.Position.Trim(),
                Salary = dto.Salary
            };
            await context.Employees.AddAsync(newEmployee);
            await context.SaveChangesAsync();
            // Invalidate cache so subsequent requests get fresh data
            await _cache.RemoveAsync(EmployeesCacheKey);
            return Ok(newEmployee);
        }

        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<Employee>> GetByEmail(string email)
        {
            var emp = await context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Email == email);
            if (emp == null)
            {
                return NotFound(new { message = "Employee not found" });
            }
            return Ok(emp);
        }
    }
}
