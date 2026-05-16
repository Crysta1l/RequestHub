using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RequestHub.Data;
using RequestHub.DTOs;
using RequestHub.Models;
using System.Security.Claims;

namespace RequestHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccessRequestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public AccessRequestController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRequestDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var request = _mapper.Map<AccessRequest>(dto);

            request.CreatedBy = userId;
            request.Status = "Draft";
            request.CreatedAt = DateTime.UtcNow;

            _context.AccessRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(request);


        }
    }
}
