using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RequestHub.DTOs;
using RequestHub.Interfaces;
using RequestHub.Models;

namespace RequestHub.Controllers
{
    [ApiController]
    [Route("api/controller")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthController(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _userRepository.GetByEmailAsync(dto.Email) != null)
                return BadRequest("Email already exists");


            var user = new User
            {
                Email = dto.Email,
                HashPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Requested"
            };

            await _userRepository.AddAsync(user);

            return Ok( new {user.Id, user.Email, user.Role }  );

        }
    }
}
