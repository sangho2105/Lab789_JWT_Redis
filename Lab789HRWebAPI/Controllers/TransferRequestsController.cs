using Lab789HRWebAPI.Core.Dtos;
using Lab789HRWebAPI.Core.Entities;
using Lab789HRWebAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;

namespace Lab789HRWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires JWT authentication
    public class TransferRequestsController : ControllerBase
    {
        private readonly HRDBContext _context;
        private readonly IDistributedCache _cache;
        private const string EmployeesCacheKey = "employees_list";

        public TransferRequestsController(HRDBContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // 1. Employee submits a department transfer request
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateTransferRequestDto dto)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) 
                            ?? User.FindFirstValue(ClaimTypes.Name)
                            ?? User.FindFirst("email")?.Value
                            ?? User.FindFirst("unique_name")?.Value
                            ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized();

            userEmail = userEmail.Trim();
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email.ToLower() == userEmail.ToLower());
            if (employee == null)
                return NotFound(new { message = "Employee information not found for this account" });

            if (employee.Department.Equals(dto.ToDepartment.Trim(), StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Destination department must differ from current department" });

            // Check if employee already has a pending transfer request
            var hasPending = await _context.DepartmentTransferRequests
                .AnyAsync(r => r.EmployeeId == employee.Id && r.Status == "PENDING");
            if (hasPending)
                return BadRequest(new { message = "You already have a transfer request pending approval!" });

            var targetPosition = string.IsNullOrWhiteSpace(dto.ToPosition) ? employee.Position : dto.ToPosition.Trim();

            var request = new DepartmentTransferRequest
            {
                EmployeeId = employee.Id,
                EmployeeEmail = employee.Email,
                FromDepartment = employee.Department,
                FromPosition = employee.Position,
                ToDepartment = dto.ToDepartment.Trim(),
                ToPosition = targetPosition,
                Reason = dto.Reason,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            await _context.DepartmentTransferRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            return Ok(request);
        }

        // 2. Employee views their own transfer requests
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) 
                            ?? User.FindFirstValue(ClaimTypes.Name)
                            ?? User.FindFirst("email")?.Value
                            ?? User.FindFirst("unique_name")?.Value
                            ?? User.Identity?.Name;

            userEmail = userEmail?.Trim();
            var requests = await _context.DepartmentTransferRequests
                .Where(r => r.EmployeeEmail.ToLower() == userEmail.ToLower())
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(requests);
        }

        // 3. Manager/Admin views pending transfer requests
        [HttpGet("pending")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var requests = await _context.DepartmentTransferRequests
                .Where(r => r.Status == "PENDING")
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            return Ok(requests);
        }

        // 4. Manager/Admin reviews (approves or rejects) a transfer request
        [HttpPut("{id}/review")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> ReviewRequest(int id, [FromBody] ReviewTransferRequestDto dto)
        {
            var reviewerEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name);

            // Use transaction to ensure atomicity when updating request and employee department/position
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.DepartmentTransferRequests.FindAsync(id);
                if (request == null)
                    return NotFound(new { message = "Transfer request not found" });

                if (request.Status != "PENDING")
                    return BadRequest(new { message = "This request has already been processed" });

                request.ReviewerEmail = reviewerEmail;
                request.ReviewNote = dto.ReviewNote;
                request.ReviewedAt = DateTime.UtcNow;

                if (dto.IsApproved)
                {
                    request.Status = "APPROVED";

                    // Update employee department and position
                    var employee = await _context.Employees.FindAsync(request.EmployeeId);
                    if (employee != null)
                    {
                        employee.Department = request.ToDepartment;
                        if (!string.IsNullOrEmpty(dto.ApprovedPosition))
                        {
                            employee.Position = dto.ApprovedPosition.Trim();
                        }
                        else if (!string.IsNullOrEmpty(request.ToPosition))
                        {
                            employee.Position = request.ToPosition.Trim();
                        }
                        _context.Employees.Update(employee);
                    }
                }
                else
                {
                    request.Status = "REJECTED";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // If approved, evict Redis cache so updated data is immediately reflected
                if (dto.IsApproved)
                {
                    await _cache.RemoveAsync(EmployeesCacheKey);
                }

                return Ok(new { message = dto.IsApproved ? "Department transfer approved successfully" : "Transfer request rejected", request });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Error processing transfer request", error = ex.Message });
            }
        }
    }
}