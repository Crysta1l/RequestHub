using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Data;
using RequestHub.DTOs;
using RequestHub.Interfaces;
using RequestHub.Models;
using System.Security.Claims;

namespace RequestHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccessRequestController : ControllerBase
    {
        private readonly IAccessRequestRepository _requestRepo;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public AccessRequestController(IAccessRequestRepository requestRepo, AppDbContext context, IMapper mapper)
        {
            _requestRepo = requestRepo;
            _context = context;
            _mapper = mapper;
        }

        private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string GetCurrentUserRole() => User.FindFirstValue(ClaimTypes.Role) ?? "";

        [HttpGet]
        public async Task<IActionResult> GetMyRequests([FromQuery] string? status = null, [FromQuery] string? resource = null)
        {
            var userId = GetCurrentUserId();
            var query = _context.AccessRequests.Where(r => r.CreatedBy == userId);
            if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);
            if (!string.IsNullOrEmpty(resource)) query = query.Where(r => r.Resource.Contains(resource));
            return Ok(await query.OrderByDescending(r => r.CreatedAt).ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequest(int id)
        {
            var request = await _requestRepo.GetByIdAsync(id);
            if (request == null) return NotFound();
            return Ok(request);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRequestDto dto)
        {
            var request = _mapper.Map<AccessRequest>(dto);
            request.CreatedBy = GetCurrentUserId();
            request.Status = "Draft";
            request.CreatedAt = DateTime.UtcNow;
            await _requestRepo.AddAsync(request);
            return Ok(request);
        }

        [HttpPatch("{id}/submit")]
        public async Task<IActionResult> Submit(int id)
        {
            var userId = GetCurrentUserId();
            var request = await _requestRepo.GetByIdAsync(id);
            if (request == null) return NotFound();
            if (request.CreatedBy != userId) return Forbid();
            if (request.Status != "Draft") return BadRequest("Only draft requests can be submitted");

            request.Status = "Submitted";
            await _requestRepo.UpdateAsync(request);
            return Ok(request);
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] string? comment)
        {
            var role = GetCurrentUserRole();
            if (!role.Equals("Approver", StringComparison.OrdinalIgnoreCase) && !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, "Only approvers or admin can approve.");

            var request = await _requestRepo.GetByIdAsync(id);
            if (request == null) return NotFound();
            if (request.Status != "Submitted") return BadRequest($"Cannot approve: status is {request.Status}.");

            request.Status = "Approved";
            await _requestRepo.UpdateAsync(request);
            return Ok(request);
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] string? comment)
        {
            var role = GetCurrentUserRole();
            if (!role.Equals("Approver", StringComparison.OrdinalIgnoreCase) && !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, "Only approvers or admin can reject.");

            var request = await _requestRepo.GetByIdAsync(id);
            if (request == null) return NotFound();
            if (request.Status != "Submitted") return BadRequest($"Cannot reject: status is {request.Status}.");

            request.Status = "Rejected";
            await _requestRepo.UpdateAsync(request);
            return Ok(request);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingForCurrentUser()
        {
            var role = GetCurrentUserRole();
            if (!role.Equals("Approver", StringComparison.OrdinalIgnoreCase) && !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return Ok(new List<AccessRequest>());

            var requests = await _context.AccessRequests.Where(r => r.Status == "Submitted").ToListAsync();
            return Ok(requests);
        }

        [HttpGet("report")]
        public async Task<IActionResult> GetReport()
        {
            var userId = GetCurrentUserId();
            var requests = await _context.AccessRequests.Where(r => r.CreatedBy == userId).ToListAsync();
            var csv = "Id,Title,Resource,AccessType,Status,CreatedAt\n" +
                string.Join("\n", requests.Select(r => $"{r.Id},{r.Title},{r.Resource},{r.AccessType},{r.Status},{r.CreatedAt:yyyy-MM-dd HH:mm}"));
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "requests.csv");
        }
    }
}