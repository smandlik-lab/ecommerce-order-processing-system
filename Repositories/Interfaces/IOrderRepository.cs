using EcommerceOrderProcessing.Models;
using EcommerceOrderProcessing.Repositories.Generic;

namespace EcommerceOrderProcessing.Repositories.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<List<Order>> GetOrdersByUserIdAsync(int userId);
        Task<List<Order>> GetAllOrdersAsync();
        Task<List<Order>> GetAllOrdersByStatusAsync(OrderStatus? status = null);
        Task<Order?> GetByIdWithItemsAndProductsAsync(int id);
        Task<Cart?> GetCartWithItemsAndProductsByUserIdAsync(int userId);
        Task<List<Order>> GetAllOrdersForAdminAsync();
        Task SaveChangesAsync();

        void RemoveCartItemsRange(IEnumerable<CartItem> cartItems);
    }
}