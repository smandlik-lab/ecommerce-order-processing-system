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

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            try
            {
                await _authService.ForgotPasswordAsync(dto);
                return Ok(new { message = "Password changed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("logout")]
        public ActionResult Logout()
        {
            var message = _authService.Logout();
            return Ok(new { message });
        }

        [Authorize]
        [HttpPatch("profile")]
        public async Task<IActionResult> PatchProfile([FromBody] UpdateProfileDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid data.");

            if (dto.FullName == null && dto.PhoneNumber == null)
                return BadRequest("Provide at least one field.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            try
            {
                var result = await _authService.PatchProfileAsync(userId, dto);

                if (!result)
                    return NotFound("User not found.");

                return Ok(new { message = "Profile updated successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPatch("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid data.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            try
            {
                var result = await _authService.ChangePasswordAsync(userId, dto);

                if (!result)
                    return NotFound("User not found.");

                return Ok(new { message = "Password changed successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPatch("change-email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            try
            {
                var result = await _authService.ChangeEmailAsync(userId, dto);

                if (!result)
                    return NotFound("User not found.");

                return Ok(new { message = "Email updated successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
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