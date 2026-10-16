using EcommerceOrderProcessing.Data;
using EcommerceOrderProcessing.Models;
using EcommerceOrderProcessing.Repositories.Generic;
using EcommerceOrderProcessing.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcommerceOrderProcessing.Repositories.Implementations
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByPhoneNumberAsync(string phoneNumber)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        }
    }
}
