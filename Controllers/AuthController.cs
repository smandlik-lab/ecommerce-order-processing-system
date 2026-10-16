using EcommerceOrderProcessing.DTOs;
using EcommerceOrderProcessing.DTOs.Auth;
using EcommerceOrderProcessing.Repositories.Interfaces;
using EcommerceOrderProcessing.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace EcommerceOrderProcessing.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;

        public AuthController(IAuthService authService, IUserRepository userRepository)
        {
            _authService = authService;
            _userRepository = userRepository;
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterDto regDto)
        {
            try
            {
                await _authService.RegisterAsync(regDto);
                return Ok(new { message = "User registered successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginDto logDto)
        {
            try
            {
                var token = await _authService.LoginAsync(logDto);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            var user = await _userRepository.GetByIdAsync(int.Parse(userId));

            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.PhoneNumber
            });
        }
    }
}