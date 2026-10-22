using EcommerceOrderProcessing.Data;
using EcommerceOrderProcessing.Repositories.Interfaces;

namespace EcommerceOrderProcessing.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;


        public IUserRepository Users { get; }
        public IOrderRepository Orders { get; }

        public UnitOfWork(AppDbContext context,
            IUserRepository userRepository,
            IOrderRepository orderRepository)
        {
            _context = context;
            Users = userRepository;
            Orders = orderRepository;
        }        

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
