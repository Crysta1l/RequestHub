using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Data;
using RequestHub.DTOs;
using RequestHub.Interfaces;
using RequestHub.Models;
using System.Security.Claims;
using System.Text;

namespace RequestHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccessRequestController : ControllerBase
    {
        private readonly IAccessRequestRepository _requestRepo;
        private readonly IUserRepository _userRepo;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public AccessRequestController(IAccessRequestRepository requestRepo, IUserRepository userRepo, AppDbContext context, IMapper mapper)
        {
            _requestRepo = requestRepo;
            _userRepo = userRepo;
            _context = context;
            _mapper = mapper;
        }

        private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string GetCurrentUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

        [HttpGet]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = GetCurrentUserId();
            var requests = await _requestRepo.GetByUserIdAsync(userId);
            return Ok(requests);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequest(int id)
        {
            var userId = GetCurrentUserId();
            var request = await _requestRepo.GetByIdAsync(id);
            if (request == null) return NotFound();
            if (request.CreatedBy != userId && GetCurrentUserRole() != "Admin") return Forbid();
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

            // Create approval steps – hardcoded approver IDs (replace with real logic)
            // Here we assign approvers: user IDs 2 and 3 (must exist in Users table)
            var approverIds = new[] { 2, 3 };
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

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] string? comment)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            if (role != "Approver" && role != "Admin") return Forbid("Only approvers can approve");

            var request = await _requestRepo.GetByIdAsync(id);
            if (request == null) return NotFound();
            if (request.Status != "Submitted") return BadRequest("Request not in submitted state");

            // Find current pending step for this user
            var step = await _context.ApprovalSteps
                .FirstOrDefaultAsync(s => s.RequestId == id && s.ApproverId == userId && s.Status == "Pending");
            if (step == null) return BadRequest("No pending approval step for you");

            step.Status = "Approved";
            step.Comment = comment;
            step.ApprovedAt = DateTime.UtcNow;
            _context.ApprovalSteps.Update(step);
            await _context.SaveChangesAsync();

            // Check if all steps are approved
            var allSteps = await _context.ApprovalSteps.Where(s => s.RequestId == id).ToListAsync();
            if (allSteps.All(s => s.Status == "Approved"))
            {
                request.Status = "Approved";
                await _requestRepo.UpdateAsync(request);
            }
            return Ok(request);
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] string? comment)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            if (role != "Approver" && role != "Admin") return Forbid("Only approvers can reject");

            var request = await _requestRepo.GetByIdAsync(id);
            if (request == null) return NotFound();
            if (request.Status != "Submitted") return BadRequest("Request not in submitted state");

            var step = await _context.ApprovalSteps
                .FirstOrDefaultAsync(s => s.RequestId == id && s.ApproverId == userId && s.Status == "Pending");
            if (step == null) return BadRequest("No pending approval step for you");

            step.Status = "Rejected";
            step.Comment = comment;
            step.ApprovedAt = DateTime.UtcNow;
            _context.ApprovalSteps.Update(step);
            request.Status = "Rejected";
            await _requestRepo.UpdateAsync(request);
            await _context.SaveChangesAsync();
            return Ok(request);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingForCurrentUser()
        {
            var userId = GetCurrentUserId();
            var steps = await _context.ApprovalSteps
                .Include(s => s.Request)
                .Where(s => s.ApproverId == userId && s.Status == "Pending")
                .Select(s => s.Request)
                .ToListAsync();
            return Ok(steps);
        }


        [HttpGet]
        public async Task<IActionResult> GetMyRequests(
        [FromQuery] string? status = null,
        [FromQuery] string? resource = null)
        {
            var userId = GetCurrentUserId();
            var query = _context.AccessRequests.Where(r => r.CreatedBy == userId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);
            if (!string.IsNullOrEmpty(resource))
                query = query.Where(r => r.Resource.Contains(resource));

            var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return Ok(requests);
        }


        [HttpGet("report")]
        public async Task<IActionResult> GetReport()
        {
            var userId = GetCurrentUserId();
            var requests = await _context.AccessRequests
                .Where(r => r.CreatedBy == userId)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Title,Resource,AccessType,Status,CreatedAt");
            foreach (var r in requests)
            {
                csv.AppendLine($"{r.Id},{r.Title},{r.Resource},{r.AccessType},{r.Status},{r.CreatedAt:yyyy-MM-dd HH:mm}");
            }
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "requests.csv");
        }


        [HttpPost("{id}/upload")]
        public async Task<IActionResult> UploadFile(int id, IFormFile file)
        {
            var request = await _requestRepo.GetByIdAsync(id);
            if (request == null) return NotFound();
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
            var filePath = Path.Combine(uploads, $"{id}_{file.FileName}");
            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);
            return Ok(new { filePath });
        }

    }
}