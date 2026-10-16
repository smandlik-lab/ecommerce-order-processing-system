using EcommerceOrderProcessing.DTOs;
using EcommerceOrderProcessing.DTOs.Auth;

namespace EcommerceOrderProcessing.Services.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterDto regDto);
        Task<string> LoginAsync(LoginDto logDto);
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<bool> PatchProfileAsync(int userId, UpdateProfileDto dto);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
        Task<bool> ChangeEmailAsync(int userId, ChangeEmailDto dto);
        string Logout();
    }
}