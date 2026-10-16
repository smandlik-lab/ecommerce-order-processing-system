using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EcommerceOrderProcessing.Controllers;
using EcommerceOrderProcessing.DTOs.Auth;
using EcommerceOrderProcessing.Models;
using EcommerceOrderProcessing.Services.Interfaces;
using EcommerceOrderProcessing.Repositories.Interfaces;
using EcommerceOrderProcessing.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace EcommerceOrderProcessing.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<AuthController>>();

            _controller = new AuthController(
                _mockAuthService.Object,
                _mockUserRepository.Object,
                _mockLogger.Object
            );
        }

        #region Register Tests

        [Fact]
        public async Task Register_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var registerDto = TestDataFactory.CreateRegisterDto();
            _mockAuthService.Setup(s => s.RegisterAsync(registerDto))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.StatusCode.Should().Be(200);
            _mockAuthService.Verify(s => s.RegisterAsync(registerDto), Times.Once);
        }

        [Fact]
        public async Task Register_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            var registerDto = TestDataFactory.CreateRegisterDto();
            _mockAuthService.Setup(s => s.RegisterAsync(registerDto))
                .ThrowsAsync(new Exception("Email already exists."));

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest?.StatusCode.Should().Be(400);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnOkWithToken()
        {
            // Arrange
            var loginDto = TestDataFactory.CreateLoginDto();
            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
            _mockAuthService.Setup(s => s.LoginAsync(loginDto))
                .ReturnsAsync(token);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.StatusCode.Should().Be(200);
            _mockAuthService.Verify(s => s.LoginAsync(loginDto), Times.Once);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginDto = TestDataFactory.CreateLoginDto();
            _mockAuthService.Setup(s => s.LoginAsync(loginDto))
                .ThrowsAsync(new Exception("Invalid email or password."));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = result as UnauthorizedObjectResult;
            unauthorizedResult?.StatusCode.Should().Be(401);
        }

        #endregion

        #region GetCurrentUser Tests

        [Fact]
        public async Task GetCurrentUser_WithValidUser_ShouldReturnOkWithUserData()
        {
            // Arrange
            var user = TestDataFactory.CreateTestUser();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.StatusCode.Should().Be(200);
            _mockUserRepository.Verify(r => r.GetByIdAsync(user.Id), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUser_WithoutUserId_ShouldReturnUnauthorized()
        {
            // Arrange
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task GetCurrentUser_WithNonExistentUser_ShouldReturnNotFound()
        {
            // Arrange
            var userId = 999;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetCurrentUser_WithException_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = 1;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult?.StatusCode.Should().Be(500);
        }

        #endregion
    }
}
