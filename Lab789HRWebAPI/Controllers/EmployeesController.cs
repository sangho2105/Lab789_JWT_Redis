using Lab789HRWebAPI.Core.Dtos;
using Lab789HRWebAPI.Core.Entities;
using Lab789HRWebAPI.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Lab789HRWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly HRDBContext context;
        public EmployeesController(HRDBContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> GetAll()
        {
            var response = await context.Employees.AsNoTracking().OrderByDescending(e => e.Salary).ToListAsync();
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<Employee>> Create(EmployeeDto dto)
        {
            var empExist = await context.Employees.AnyAsync(e => e.Email == dto.Email);
            if (empExist)
            {
                return Conflict(new 
                {
                    message = "Email already"
                });
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
            return Ok(newEmployee);
        }
    }
}
