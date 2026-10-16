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

        public AuthService(IUserRepository userRepository, IConfiguration config, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _config = config;
            _unitOfWork = unitOfWork;
        }

        public async Task RegisterAsync(RegisterDto regDto)
        {
            var emailExists = await _userRepository.EmailExistsAsync(regDto.Email);
            if (emailExists)
                throw new Exception("Email already exists.");

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
        }

        public async Task<string> LoginAsync(LoginDto logDto)
        {
            var user = await _userRepository.GetByEmailAsync(logDto.Email);

            if (user == null)
                throw new Exception("Invalid email or password.");

            var hashedPassword = HashPassword(logDto.Password);

            if (user.PasswordHash != hashedPassword)
                throw new Exception("Invalid email or password.");

            return GenerateToken(user);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            if (user == null)
                throw new Exception("User not found.");

            if (user.PhoneNumber != dto.PhoneNumber)
                throw new Exception("Email or phone number is incorrect.");

            user.PasswordHash = HashPassword(dto.NewPassword);

            await _unitOfWork.SaveChangesAsync();
        }

        public string Logout()
        {
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
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> PatchProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
                return false;

            if (dto.FullName != null)
            {
                if (string.IsNullOrWhiteSpace(dto.FullName))
                    throw new ArgumentException("Full name cannot be empty.");

                user.FullName = dto.FullName.Trim();
            }

            if (dto.PhoneNumber != null)
            {
                var phone = dto.PhoneNumber.Trim();

                if (string.IsNullOrWhiteSpace(phone))
                    throw new ArgumentException("Phone number cannot be empty.");

                if (phone.Length != 11 || !phone.All(char.IsDigit) || !phone.StartsWith("01"))
                    throw new ArgumentException("Phone number must be a valid Egyptian phone number.");

                var existingUser = await _userRepository.GetByPhoneNumberAsync(phone);

                if (existingUser != null && existingUser.Id != userId)
                    throw new ArgumentException("Phone number is already in use.");

                user.PhoneNumber = phone;
            }

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
                return false;

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword) ||
                string.IsNullOrWhiteSpace(dto.NewPassword) ||
                string.IsNullOrWhiteSpace(dto.ConfirmNewPassword))
            {
                throw new ArgumentException("All password fields are required.");
            }

            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new ArgumentException("New password and confirm password do not match.");

            if (HashPassword(dto.CurrentPassword) != user.PasswordHash)
                throw new ArgumentException("Current password is incorrect.");

            if (dto.NewPassword == dto.CurrentPassword)
                throw new ArgumentException("New password must be different from current password.");

            if (dto.NewPassword.Length < 6)
                throw new ArgumentException("New password must be at least 6 characters long.");

            user.PasswordHash = HashPassword(dto.NewPassword);

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeEmailAsync(int userId, ChangeEmailDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
                return false;

            if (string.IsNullOrWhiteSpace(dto.NewEmail))
                throw new ArgumentException("Email is required.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Password is required.");

            if (HashPassword(dto.Password) != user.PasswordHash)
                throw new ArgumentException("Incorrect password.");

            var emailExists = await _userRepository.EmailExistsAsync(dto.NewEmail);
            if (emailExists)
                throw new ArgumentException("Email already exists.");

            user.Email = dto.NewEmail;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}