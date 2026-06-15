using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Data;
using System.Security.Claims;

namespace RequestHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        private string GetCurrentUserRole() => User.FindFirstValue(ClaimTypes.Role) ?? "";

        // Get all users — Admin only
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? email = null)
        {
            if (!GetCurrentUserRole().Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, "Only admin can view users.");

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(email))
                query = query.Where(u => u.Email.Contains(email));

            var users = await query
                .OrderBy(u => u.Email)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Role,
                    u.IsActive,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // Change user role — Admin only
        [HttpPatch("users/{id}/role")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] string role)
        {
            if (!GetCurrentUserRole().Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, "Only admin can change roles.");

            var allowedRoles = new[] { "Requester", "Approver", "Admin" };
            if (!allowedRoles.Contains(role))
                return BadRequest("Invalid role. Allowed: Requester, Approver, Admin");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Role = role;
            await _context.SaveChangesAsync();
            return Ok(new { user.Id, user.Email, user.Role });
        }

        // Deactivate user — Admin only
        [HttpPatch("users/{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            if (!GetCurrentUserRole().Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, "Only admin can deactivate users.");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"User {user.Email} deactivated" });
        }

        // Activate user — Admin only
        [HttpPatch("users/{id}/activate")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            if (!GetCurrentUserRole().Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, "Only admin can activate users.");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"User {user.Email} activated" });
        }
    }
}