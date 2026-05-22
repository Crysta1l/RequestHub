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

        // Helper to log history
        private void LogHistory(int requestId, string action, string? oldStatus, string? newStatus)
        {
            _context.RequestHistories.Add(new RequestHistory
            {
                RequestId = requestId,
                Action = action,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                PerformedById = GetCurrentUserId(),
                PerformedAt = DateTime.UtcNow
            });
        }

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

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetHistory(int id)
        {
            var history = await _context.RequestHistories
                .Where(h => h.RequestId == id)
                .OrderByDescending(h => h.PerformedAt)
                .ToListAsync();
            return Ok(history);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRequestDto dto)
        {
            var request = _mapper.Map<AccessRequest>(dto);
            request.CreatedBy = GetCurrentUserId();
            request.Status = "Draft";
            request.CreatedAt = DateTime.UtcNow;
            await _requestRepo.AddAsync(request);

            // Log creation
            LogHistory(request.Id, "Created", null, "Draft");
            await _context.SaveChangesAsync();

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

            // Create 2 approval steps: step 1 for Approver, step 2 for Admin
            _context.ApprovalSteps.AddRange(
                new ApprovalStep { RequestId = id, Order = 1, Status = "Pending" },
                new ApprovalStep { RequestId = id, Order = 2, Status = "Pending" }
            );

            LogHistory(id, "StatusChanged", "Draft", "Submitted");
            request.Status = "Submitted";
            await _requestRepo.UpdateAsync(request);
            await _context.SaveChangesAsync();
            return Ok(request);
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] CommentDto? dto)
        {
            var role = GetCurrentUserRole();
            if (!role.Equals("Approver", StringComparison.OrdinalIgnoreCase) &&
                !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, "Only approvers or admin can approve.");

            var request = await _requestRepo.GetByIdAsync(id);
            if (request == null) return NotFound();

            var steps = await _context.ApprovalSteps
                .Where(s => s.RequestId == id)
                .OrderBy(s => s.Order)
                .ToListAsync();

            var oldStatus = request.Status;

            if (role.Equals("Approver", StringComparison.OrdinalIgnoreCase))
            {
                if (request.Status != "Submitted")
                    return BadRequest("Request must be in Submitted status for Approver.");

                var step = steps.FirstOrDefault(s => s.Order == 1);
                if (step == null || step.Status != "Pending")
                    return BadRequest("Approval step not available.");

                step.Status = "Approved";
                step.ApproverId = GetCurrentUserId();
                step.ApprovedAt = DateTime.UtcNow;
                step.Comment = dto?.Comment;
                request.Status = "InApproval";
            }
            else if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var step1 = steps.FirstOrDefault(s => s.Order == 1);
                if (step1?.Status != "Approved")
                    return BadRequest("Approver must approve first before Admin.");

                if (request.Status != "InApproval")
                    return BadRequest("Request must be in InApproval status for Admin.");

                var step2 = steps.FirstOrDefault(s => s.Order == 2);
                if (step2 == null || step2.Status != "Pending")
                    return BadRequest("Approval step not available.");

                step2.Status = "Approved";
                step2.ApproverId = GetCurrentUserId();
                step2.ApprovedAt = DateTime.UtcNow;
                step2.Comment = dto?.Comment;
                request.Status = "Approved";
            }

            // Log comment if provided
            if (!string.IsNullOrEmpty(dto?.Comment))
                LogHistory(id, "CommentAdded", null, null);

            LogHistory(id, "StatusChanged", oldStatus, request.Status);
            await _context.SaveChangesAsync();
            return Ok(request);
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] CommentDto? dto)
        {
            var role = GetCurrentUserRole();
            if (!role.Equals("Approver", StringComparison.OrdinalIgnoreCase) &&
                !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, "Only approvers or admin can reject.");

            var request = await _requestRepo.GetByIdAsync(id);
            if (request == null) return NotFound();

            if (request.Status != "Submitted" && request.Status != "InApproval")
                return BadRequest($"Cannot reject: status is {request.Status}.");

            var steps = await _context.ApprovalSteps
                .Where(s => s.RequestId == id)
                .OrderBy(s => s.Order)
                .ToListAsync();

            var currentStep = steps.FirstOrDefault(s => s.Status == "Pending");
            if (currentStep != null)
            {
                currentStep.Status = "Rejected";
                currentStep.ApproverId = GetCurrentUserId();
                currentStep.ApprovedAt = DateTime.UtcNow;
                currentStep.Comment = dto?.Comment;
            }

            var oldStatus = request.Status;
            request.Status = "Rejected";

            if (!string.IsNullOrEmpty(dto?.Comment))
                LogHistory(id, "CommentAdded", null, null);

            LogHistory(id, "StatusChanged", oldStatus, "Rejected");
            await _context.SaveChangesAsync();
            return Ok(request);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingForCurrentUser()
        {
            var role = GetCurrentUserRole();

            // Approver sees Submitted requests (step 1)
            if (role.Equals("Approver", StringComparison.OrdinalIgnoreCase))
            {
                var requests = await _context.AccessRequests
                    .Where(r => r.Status == "Submitted")
                    .ToListAsync();
                return Ok(requests);
            }

            // Admin sees InApproval requests (step 2)
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var requests = await _context.AccessRequests
                    .Where(r => r.Status == "InApproval")
                    .ToListAsync();
                return Ok(requests);
            }

            return Ok(new List<AccessRequest>());
        }

        [HttpGet("report")]
        public async Task<IActionResult> GetReport()
        {
            var userId = GetCurrentUserId();
            var requests = await _context.AccessRequests.Where(r => r.CreatedBy == userId).ToListAsync();
            var csv = "Id,Title,Resource,AccessType,Priority,Department,Status,CreatedAt,ExpiryDate\n" +
                string.Join("\n", requests.Select(r =>
                    $"{r.Id},{r.Title},{r.Resource},{r.AccessType},{r.Priority},{r.Department},{r.Status},{r.CreatedAt:yyyy-MM-dd HH:mm},{r.ExpiryDate:yyyy-MM-dd}"));
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "requests.csv");
        }
    }

    // DTO for comment body
    public record CommentDto(string? Comment);
}