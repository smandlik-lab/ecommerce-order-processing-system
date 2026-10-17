using EcommerceOrderProcessing.DTOs.Auth;
using EcommerceOrderProcessing.Repositories.Interfaces;
using EcommerceOrderProcessing.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace EcommerceOrderProcessing.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;

        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="authService">The authentication service used for handling user registration and login.</param>
        /// <param name="userRepository">The user repository used for accessing user data.</param>
        /// <param name="logger">The logger used for logging information and errors.</param>
        public AuthController(IAuthService authService, IUserRepository userRepository, ILogger<AuthController> logger)
        {
            _authService = authService;
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user in the system.
        /// </summary>
        /// <param name="regDto">The registration details of the new user.</param>
        /// <returns>An <see cref="ActionResult"/> indicating the result of the registration operation.</returns>
        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterDto regDto)
        {
            try
            {
                _logger.LogInformation("Registration request received for email: {Email}", regDto.Email);
                await _authService.RegisterAsync(regDto);
                _logger.LogInformation("User registered successfully. Email: {Email}", regDto.Email);
                return Ok(new { message = "User registered successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during registration for email: {Email}. Details: {ErrorMessage}", regDto.Email, ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Authenticates a user and generates a JWT token.
        /// </summary>
        /// <param name="logDto">The login details of the user.</param>
        /// <returns>An <see cref="ActionResult"/> containing the JWT token if authentication is successful.</returns>
        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginDto logDto)
        {
            try
            {
                _logger.LogInformation("Login request received for email: {Email}", logDto.Email);
                var token = await _authService.LoginAsync(logDto);
                _logger.LogInformation("User logged in successfully. Email: {Email}", logDto.Email);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for email: {Email}. Details: {ErrorMessage}", logDto.Email, ex.Message);
                return Unauthorized(ex.Message);
            }
        }
        /// <summary>
        /// Retrieves the details of the currently authenticated user.
        /// </summary>
        /// <returns>An <see cref="ActionResult"/> containing the current user's details.</returns>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                _logger.LogInformation("GetCurrentUser request initiated.");
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (userId == null)
                {
                    _logger.LogWarning("GetCurrentUser failed: User ID not found in claims.");
                    return Unauthorized();
                }

                _logger.LogInformation("Fetching user details for UserId: {UserId}", userId);
                var user = await _userRepository.GetByIdAsync(int.Parse(userId));

                if (user == null)
                {
                    _logger.LogWarning("GetCurrentUser failed: User not found for UserId: {UserId}", userId);
                    return NotFound();
                }

                _logger.LogInformation("User details retrieved successfully for UserId: {UserId}", userId);
                return Ok(new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving current user. Details: {ErrorMessage}", ex.Message);
                return StatusCode(500, "An error occurred while retrieving user information.");
            }
        }
    }
}