using EcommerceOrderProcessing.DTOs;
using EcommerceOrderProcessing.DTOs.Auth;
using EcommerceOrderProcessing.Models;
using EcommerceOrderProcessing.Repositories.Implementations;
using EcommerceOrderProcessing.Repositories.Interfaces;
using EcommerceOrderProcessing.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EcommerceOrderProcessing.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository, IConfiguration config, IUnitOfWork unitOfWork, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _config = config;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task RegisterAsync(RegisterDto regDto)
        {
            try
            {
                _logger.LogInformation("Registration process started for email: {Email}", regDto.Email);
                var emailExists = await _userRepository.EmailExistsAsync(regDto.Email);
                if (emailExists)
                {
                    _logger.LogWarning("Registration failed: Email already exists. Email: {Email}", regDto.Email);
                    throw new Exception("Email already exists.");
                }

                var user = new User
                {
                    FullName = regDto.FullName,
                    Email = regDto.Email,
                    PasswordHash = HashPassword(regDto.Password),
                    PhoneNumber = regDto.PhoneNumber,
                    Role = UserRole.Customer
                };

                await _userRepository.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("User registered successfully. Email: {Email}, UserId: {UserId}", regDto.Email, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during registration for email: {Email}. Details: {ErrorMessage}", regDto.Email, ex.Message);
                throw;
            }
        }

        public async Task<string> LoginAsync(LoginDto logDto)
        {
            try
            {
                _logger.LogInformation("Login process started for email: {Email}", logDto.Email);
                var user = await _userRepository.GetByEmailAsync(logDto.Email);

                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found for email: {Email}", logDto.Email);
                    throw new Exception("Invalid email or password.");
                }

                var hashedPassword = HashPassword(logDto.Password);

                if (user.PasswordHash != hashedPassword)
                {
                    _logger.LogWarning("Login failed: Invalid password for email: {Email}", logDto.Email);
                    throw new Exception("Invalid email or password.");
                }

                var token = GenerateToken(user);
                _logger.LogInformation("User logged in successfully. Email: {Email}, UserId: {UserId}", logDto.Email, user.Id);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for email: {Email}. Details: {ErrorMessage}", logDto.Email, ex.Message);
                throw;
            }
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            try
            {
                _logger.LogInformation("Forgot password process started for email: {Email}", dto.Email);
                var user = await _userRepository.GetByEmailAsync(dto.Email);

                if (user == null)
                {
                    _logger.LogWarning("Forgot password failed: User not found for email: {Email}", dto.Email);
                    throw new Exception("User not found.");
                }

                if (user.PhoneNumber != dto.PhoneNumber)
                {
                    _logger.LogWarning("Forgot password failed: Phone number mismatch for email: {Email}", dto.Email);
                    throw new Exception("Email or phone number is incorrect.");
                }

                user.PasswordHash = HashPassword(dto.NewPassword);

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Password reset successfully for email: {Email}", dto.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during forgot password for email: {Email}. Details: {ErrorMessage}", dto.Email, ex.Message);
                throw;
            }
        }

        public string Logout()
        {
            _logger.LogInformation("User logged out successfully.");
            return "Logged out successfully.";
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private string GenerateToken(User user)
        {
            // JWT (JSON Web Token) consists of three parts separated by dots (.):
            // 1. HEADER - Contains the token type (JWT) and the hashing algorithm (HMAC SHA256)
            // 2. PAYLOAD - Contains claims (user information) encoded in Base64Url
            // 3. SIGNATURE - Cryptographic signature to verify token hasn't been tampered with
            // Format: header.payload.signature

            // PAYLOAD SECTION: Claims are statements about the subject (user)
            // Claims contain user identity information that will be encoded in the token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Subject identifier (sub)
                new Claim(ClaimTypes.Email, user.Email),                  // User email
                new Claim(ClaimTypes.Role, user.Role.ToString())          // User role for authorization
            };

            // SIGNATURE SECTION - Part 1: Create the signing key
            // The key is derived from the JWT secret stored in configuration
            // This secret is used to create a HMAC SHA256 signature
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
            );

            // SIGNATURE SECTION - Part 2: Define signing credentials
            // Specifies the algorithm (HmacSha256) to use for creating the digital signature
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create the complete JWT token
            // This combines the header, payload claims, and signature
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],      // Token issuer (who created it) - iss claim
                audience: _config["Jwt:Audience"],  // Token audience (intended recipient) - aud claim
                claims: claims,                      // PAYLOAD: User claims/data
                expires: DateTime.Now.AddDays(7),   // Expiration time (exp claim) - token valid for 7 days
                signingCredentials: creds           // SIGNATURE: Signing key and algorithm
            );

            // Convert the JWT object to a string token (Base64Url encoded format)
            // Result format: eyJhbGciOiJIUzI1NiI...eyJzdWIiOiIxIiwi....Ub7xWCU5gZpUtQ
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> PatchProfileAsync(int userId, UpdateProfileDto dto)
        {
            try
            {
                _logger.LogInformation("Profile update process started for UserId: {UserId}", userId);
                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("Profile update failed: User not found for UserId: {UserId}", userId);
                    return false;
                }

                if (dto.FullName != null)
                {
                    if (string.IsNullOrWhiteSpace(dto.FullName))
                    {
                        _logger.LogWarning("Profile update failed: Full name cannot be empty for UserId: {UserId}", userId);
                        throw new ArgumentException("Full name cannot be empty.");
                    }

                    user.FullName = dto.FullName.Trim();
                    _logger.LogInformation("Full name updated for UserId: {UserId}", userId);
                }

                if (dto.PhoneNumber != null)
                {
                    var phone = dto.PhoneNumber.Trim();

                    if (string.IsNullOrWhiteSpace(phone))
                    {
                        _logger.LogWarning("Profile update failed: Phone number cannot be empty for UserId: {UserId}", userId);
                        throw new ArgumentException("Phone number cannot be empty.");
                    }

                    if (phone.Length != 11 || !phone.All(char.IsDigit) || !phone.StartsWith("01"))
                    {
                        _logger.LogWarning("Profile update failed: Invalid phone number for UserId: {UserId}", userId);
                        throw new ArgumentException("Phone number must be a valid Egyptian phone number.");
                    }

                    var existingUser = await _userRepository.GetByPhoneNumberAsync(phone);

                    if (existingUser != null && existingUser.Id != userId)
                    {
                        _logger.LogWarning("Profile update failed: Phone number already in use for UserId: {UserId}", userId);
                        throw new ArgumentException("Phone number is already in use.");
                    }

                    user.PhoneNumber = phone;
                    _logger.LogInformation("Phone number updated for UserId: {UserId}", userId);
                }

                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Profile updated successfully for UserId: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during profile update for UserId: {UserId}. Details: {ErrorMessage}", userId, ex.Message);
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            try
            {
                _logger.LogInformation("Change password process started for UserId: {UserId}", userId);
                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("Change password failed: User not found for UserId: {UserId}", userId);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(dto.CurrentPassword) ||
                    string.IsNullOrWhiteSpace(dto.NewPassword) ||
                    string.IsNullOrWhiteSpace(dto.ConfirmNewPassword))
                {
                    _logger.LogWarning("Change password failed: Missing required fields for UserId: {UserId}", userId);
                    throw new ArgumentException("All password fields are required.");
                }

                if (dto.NewPassword != dto.ConfirmNewPassword)
                {
                    _logger.LogWarning("Change password failed: New password and confirm password do not match for UserId: {UserId}", userId);
                    throw new ArgumentException("New password and confirm password do not match.");
                }

                if (HashPassword(dto.CurrentPassword) != user.PasswordHash)
                {
                    _logger.LogWarning("Change password failed: Current password is incorrect for UserId: {UserId}", userId);
                    throw new ArgumentException("Current password is incorrect.");
                }

                if (dto.NewPassword == dto.CurrentPassword)
                {
                    _logger.LogWarning("Change password failed: New password same as current password for UserId: {UserId}", userId);
                    throw new ArgumentException("New password must be different from current password.");
                }

                if (dto.NewPassword.Length < 6)
                {
                    _logger.LogWarning("Change password failed: New password too short for UserId: {UserId}", userId);
                    throw new ArgumentException("New password must be at least 6 characters long.");
                }

                user.PasswordHash = HashPassword(dto.NewPassword);

                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Password changed successfully for UserId: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during password change for UserId: {UserId}. Details: {ErrorMessage}", userId, ex.Message);
                throw;
            }
        }

        public async Task<bool> ChangeEmailAsync(int userId, ChangeEmailDto dto)
        {
            try
            {
                _logger.LogInformation("Change email process started for UserId: {UserId}", userId);
                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("Change email failed: User not found for UserId: {UserId}", userId);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(dto.NewEmail))
                {
                    _logger.LogWarning("Change email failed: Email is required for UserId: {UserId}", userId);
                    throw new ArgumentException("Email is required.");
                }

                if (string.IsNullOrWhiteSpace(dto.Password))
                {
                    _logger.LogWarning("Change email failed: Password is required for UserId: {UserId}", userId);
                    throw new ArgumentException("Password is required.");
                }

                if (HashPassword(dto.Password) != user.PasswordHash)
                {
                    _logger.LogWarning("Change email failed: Incorrect password for UserId: {UserId}", userId);
                    throw new ArgumentException("Incorrect password.");
                }

                var emailExists = await _userRepository.EmailExistsAsync(dto.NewEmail);
                if (emailExists)
                {
                    _logger.LogWarning("Change email failed: Email already exists. UserId: {UserId}, NewEmail: {NewEmail}", userId, dto.NewEmail);
                    throw new ArgumentException("Email already exists.");
                }

                user.Email = dto.NewEmail;

                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Email changed successfully for UserId: {UserId}. NewEmail: {NewEmail}", userId, dto.NewEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during email change for UserId: {UserId}. Details: {ErrorMessage}", userId, ex.Message);
                throw;
            }
        }
    }
}