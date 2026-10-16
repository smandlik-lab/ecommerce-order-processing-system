using EcommerceOrderProcessing.Models;
using EcommerceOrderProcessing.Repositories.Generic;

namespace EcommerceOrderProcessing.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<bool> EmailExistsAsync(string email);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    }
}