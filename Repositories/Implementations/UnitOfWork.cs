using EcommerceOrderProcessing.Data;
using EcommerceOrderProcessing.Repositories.Interfaces;

namespace EcommerceOrderProcessing.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IOrderRepository Orders { get; }

        public UnitOfWork(AppDbContext context,
            IOrderRepository orderRepository)
        {
            _context = context;
            Orders = orderRepository;
        }        

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
