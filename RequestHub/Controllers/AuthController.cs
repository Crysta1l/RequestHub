using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RequestHub.Data;
using RequestHub.DTOs;
using RequestHub.Interfaces;
using RequestHub.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RequestHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public AuthController(IUserRepository userRepository, IConfiguration configuration, AppDbContext context)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _context = context;
        }

        private string GetIpAddress() =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        private string GetUserAgent() =>
            HttpContext.Request.Headers["User-Agent"].ToString() ?? "unknown";

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _userRepository.GetByEmailAsync(dto.Email) != null)
                return BadRequest("Email already exists");

            var user = new User
            {
                Email = dto.Email,
                HashPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Requester"
            };

            await _userRepository.AddAsync(user);
            return Ok(new { user.Id, user.Email, user.Role });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            // Log failed login attempt
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.HashPassword))
            {
                if (user != null)
                {
                    _context.AuditLogs.Add(new AuditLog
                    {
                        UserId = user.Id,
                        Action = "Login",
                        IpAddress = GetIpAddress(),
                        UserAgent = GetUserAgent(),
                        IsSuccess = false,
                        PerformedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }
                return Unauthorized("Invalid credentials");
            }

            // Log successful login
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                Action = "Login",
                IpAddress = GetIpAddress(),
                UserAgent = GetUserAgent(),
                IsSuccess = true,
                PerformedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return Ok(new { token, user.Email, user.Role });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Get user id from token
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Action = "Logout",
                    IpAddress = GetIpAddress(),
                    UserAgent = GetUserAgent(),
                    IsSuccess = true,
                    PerformedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Successfully logged out" });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}