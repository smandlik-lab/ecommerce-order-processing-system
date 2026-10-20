using Moq;
using Xunit;
using FluentAssertions;
using EcommerceOrderProcessing.DTOs.Auth;
using EcommerceOrderProcessing.Models;
using EcommerceOrderProcessing.Services.Implementations;
using EcommerceOrderProcessing.Tests.Fixtures;
using Microsoft.Extensions.Logging;

namespace EcommerceOrderProcessing.Tests.Services
{
    public class AuthServiceTests : BaseServiceTest
    {
        private readonly AuthService _authService;
        private readonly Mock<ILogger<AuthService>> _mockLogger;

        public AuthServiceTests()
        {
            _mockLogger = CreateMockLogger<AuthService>();
            SetupConfigurationMock("Jwt:Key", "ThisIsAVeryLongSecretKeyForJwtTokenGenerationThatMustBeAtLeast32CharactersLong");
            SetupConfigurationMock("Jwt:Issuer", "YourAppName");
            SetupConfigurationMock("Jwt:Audience", "YourAppUsers");

            _authService = new AuthService(
                MockUserRepository.Object,
                MockConfiguration.Object,
                MockUnitOfWork.Object,
                _mockLogger.Object
            );
        }

        #region RegisterAsync Tests

        [Fact]
        public async Task RegisterAsync_WithValidData_ShouldRegisterUserSuccessfully()
        {
            // Arrange
            var registerDto = TestDataFactory.CreateRegisterDto("newuser@example.com");
            MockUserRepository.Setup(r => r.EmailExistsAsync(registerDto.Email))
                .ReturnsAsync(false);
            MockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            await _authService.RegisterAsync(registerDto);

            // Assert
            MockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ShouldThrowException()
        {
            // Arrange
            var registerDto = TestDataFactory.CreateRegisterDto("existing@example.com");
            MockUserRepository.Setup(r => r.EmailExistsAsync(registerDto.Email))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _authService.RegisterAsync(registerDto));
            MockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldStoreHashedPassword_NotPlainText()
        {
            // Arrange
            var registerDto = TestDataFactory.CreateRegisterDto("newuser@example.com");
            User capturedUser = null!;

            MockUserRepository.Setup(r => r.EmailExistsAsync(registerDto.Email))
                .ReturnsAsync(false);
            MockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(user => capturedUser = user)
                .Returns(Task.CompletedTask);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            await _authService.RegisterAsync(registerDto);

            // Assert
            capturedUser.Should().NotBeNull();
            capturedUser.PasswordHash.Should().NotBe(registerDto.Password);
            capturedUser.Role.Should().Be(UserRole.Customer);
        }

        #endregion

        #region LoginAsync Tests

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var user = TestDataFactory.CreateTestUser(1, "test@example.com");
            var loginDto = new LoginDto { Email = "test@example.com", Password = "Password123!" };

            // Hash the password same way as the service does
            var hashedPassword = HashPassword(loginDto.Password);
            user.PasswordHash = hashedPassword;

            MockUserRepository.Setup(r => r.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);

            // Act
            var token = await _authService.LoginAsync(loginDto);

            // Assert
            token.Should().NotBeNullOrEmpty();
            token.Should().Contain(".");
        }

        [Fact]
        public async Task LoginAsync_WithNonexistentUser_ShouldThrowException()
        {
            // Arrange
            var loginDto = TestDataFactory.CreateLoginDto("nonexistent@example.com");
            MockUserRepository.Setup(r => r.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync((User)null!);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _authService.LoginAsync(loginDto));
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ShouldThrowException()
        {
            // Arrange
            var user = TestDataFactory.CreateTestUser(1, "test@example.com");
            var loginDto = new LoginDto { Email = "test@example.com", Password = "WrongPassword123!" };

            MockUserRepository.Setup(r => r.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _authService.LoginAsync(loginDto));
        }

        #endregion

        #region ForgotPasswordAsync Tests

        [Fact]
        public async Task ForgotPasswordAsync_WithValidEmail_ShouldUpdateUser()
        {
            // Arrange
            var user = TestDataFactory.CreateTestUser(1, "test@example.com");
            var dto = TestDataFactory.CreateForgotPasswordDto("test@example.com");

            MockUserRepository.Setup(r => r.GetByEmailAsync(dto.Email))
                .ReturnsAsync(user);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            await _authService.ForgotPasswordAsync(dto);

            // Assert
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_WithNonexistentEmail_ShouldThrowException()
        {
            // Arrange
            var dto = TestDataFactory.CreateForgotPasswordDto("nonexistent@example.com");
            MockUserRepository.Setup(r => r.GetByEmailAsync(dto.Email))
                .ReturnsAsync((User)null!);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _authService.ForgotPasswordAsync(dto));
        }

        #endregion

        // Helper method to hash password (mirrors AuthService logic)
        private string HashPassword(string password)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA256())
            {
                var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
