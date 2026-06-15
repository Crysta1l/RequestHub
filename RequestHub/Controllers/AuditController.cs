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
    public class AuditController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuditController(AppDbContext context)
        {
            _context = context;
        }

        private string GetCurrentUserRole() => User.FindFirstValue(ClaimTypes.Role) ?? "";

        // Get audit log — Admin only
        [HttpGet]
        public async Task<IActionResult> GetAuditLog(
            [FromQuery] string? email = null,
            [FromQuery] string? action = null)
        {
            if (!GetCurrentUserRole().Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, "Only admin can view audit log.");

            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(email))
                query = query.Where(a => a.User.Email.Contains(email));

            if (!string.IsNullOrEmpty(action))
                query = query.Where(a => a.Action == action);

            var logs = await query
                .OrderByDescending(a => a.PerformedAt)
                .Select(a => new
                {
                    a.Id,
                    a.Action,
                    a.IpAddress,
                    a.UserAgent,
                    a.IsSuccess,
                    a.PerformedAt,
                    UserEmail = a.User.Email
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}