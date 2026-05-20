using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Data;
using RequestHub.DTOs;
using RequestHub.Interfaces;
using RequestHub.Models;
using RequestHub.Repositories;
using System.Security.Claims;

namespace RequestHub.Controllers
{
    // Did some refactoring 
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccessRequestController : ControllerBase
    {
        private readonly IAccessRequestRepository _requestRepo;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;

        public AccessRequestController(IAccessRequestRepository requestRepo, IMapper mapper, AppDbContext context) 
        {
            _requestRepo = requestRepo;
            _mapper = mapper;
            _context = context;
        }

        private int GetCurrentUserID() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string GetCurrentUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

        [HttpGet]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = GetCurrentUserID();
            var request = await _requestRepo.GetByUserIdAsync(userId);

            return Ok(request);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRequestDto dto)
        {
            var request = _mapper.Map<AccessRequest>(dto);

            request.CreatedBy = GetCurrentUserID();
            request.Status = "Draft";
            request.CreatedAt = DateTime.UtcNow;

            await _requestRepo.AddAsync(request);

            return Ok(request);

        }

        [HttpPatch("{id}/submit")]
        public async Task<IActionResult> Submit(int id)
        {
            var userId = GetCurrentUserID();
            var request = await _requestRepo.GetByIdAsync(id);
            if (request == null) return NotFound();
            if (request.CreatedBy != userId) return Forbid();
            if (request.Status != "Draft") return BadRequest("Only draft requests can be submitted");

            request.Status = "Submitted";
            await _requestRepo.UpdateAsync(request);

            // Create approval steps (example: hardcoded approver IDs – replace with real logic later)
            var approverIds = new[] { 2, 3 }; // IDs of users with role "Approver"
            for (int i = 0; i < approverIds.Length; i++)
            {
                var step = new ApprovalStep
                {
                    RequestId = request.Id,
                    ApproverId = approverIds[i],
                    Order = i + 1,
                    Status = "Pending"
                };
                _context.ApprovalSteps.Add(step);
            }
            await _context.SaveChangesAsync();

            return Ok(request);
        }

        [HttpPatch("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var role = GetCurrentUserRole();

            if (role != "Approver" && role != "Admin")
                return Forbid("Only approved can approve");

            var request = await _requestRepo.GetByIdAsync(id);

            if (request == null)
                return NotFound();

            if (request.Status != "Sumbitted")
                return BadRequest("Only submitted requests can be approved");

            request.Status = "Approved";

            await _requestRepo.UpdateAsync(request);

            return Ok(request);
        }

        [HttpPatch("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] string? comment)
        {
            var role = GetCurrentUserRole();

            if (role != "Approver" && role != "Admin")
                return Forbid("Only apprivers can reject");

            var request = await _requestRepo.GetByIdAsync(id);

            if (request == null)
                return NotFound();

            if (request.Status != "Sumbitted")
                return BadRequest("Only sumbitted requests can be rejected");

            request.Status = "Rejected";

            await _requestRepo.UpdateAsync(request);

            return Ok(request);

        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingForApprover()
        {
            var role = GetCurrentUserRole();


            if (role != "Approver" && role != "Admin")
                return Forbid();

            var pending = await _context.AccessRequests
                .Where(r => r.Status == "Sumbitted")
                .ToListAsync();

            return Ok(pending);
        }
    }
}
