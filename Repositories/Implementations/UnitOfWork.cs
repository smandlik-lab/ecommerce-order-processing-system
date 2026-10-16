using EcommerceOrderProcessing.Data;
using EcommerceOrderProcessing.Repositories.Interfaces;

namespace EcommerceOrderProcessing.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IOrderRepository? _orders;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IOrderRepository Orders => _orders ??= new OrderRepository(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
