using EcommerceOrderProcessing.DTOs.OrderDTOs;
using EcommerceOrderProcessing.Models;
using EcommerceOrderProcessing.Repositories.Implementations;
using EcommerceOrderProcessing.Repositories.Interfaces;
using EcommerceOrderProcessing.Services.Interfaces;

namespace EcommerceOrderProcessing.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(IOrderRepository orderRepository, IUnitOfWork unitOfWork    )
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task CreateOrderAsync(int userId)
        {
            var cart = await _orderRepository.GetCartWithItemsAndProductsByUserIdAsync(userId);

            if (cart == null || !cart.CartItems.Any())
                throw new Exception("Cart is empty.");

            foreach (var item in cart.CartItems)
            {
                if (item.Product == null)
                    throw new Exception("Invalid product in cart.");

                if (item.Quantity > item.Product.StockQuantity)
                    throw new Exception($"Not enough stock for product: {item.Product.Name}");
            }

            decimal totalPrice = cart.CartItems.Sum(ci => ci.Product!.Price * ci.Quantity);

            var order = new Order
            {
                UserId = userId,
                TotalAmount = totalPrice,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem>()
            };

            foreach (var item in cart.CartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product!.Price
                });

                item.Product.StockQuantity -= item.Quantity;
            }

            await _orderRepository.AddAsync(order);
            _orderRepository.RemoveCartItemsRange(cart.CartItems);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<OrderResponseDto>> GetMyOrdersAsync(int userId)
        {
            var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);
            return orders.Select(MapToDto).ToList();
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int userId, int orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null || order.UserId != userId)
                return null;

            return MapToDto(order);
        }

        public async Task<object> CancelOrderAsync(int userId, int orderId)
        {
            var order = await _orderRepository.GetByIdWithItemsAndProductsAsync(orderId);

            if (order == null || order.UserId != userId)
                throw new Exception("Order not found.");

            if (order.Status == OrderStatus.Completed)
                throw new Exception("Completed orders cannot be cancelled.");

            if (order.Status == OrderStatus.Cancelled)
                throw new Exception("Order is already cancelled.");

            foreach (var item in order.OrderItems)
            {
                //if (item.Product != null)
                //    item.Product.StockQuantity += item.Quantity;
            }

            order.Status = OrderStatus.Cancelled;
            await _unitOfWork.SaveChangesAsync();

            return new
            {
                message = "Order cancelled successfully",
                orderId = order.Id,
                status = order.Status.ToString()
            };
        }

        public async Task<List<OrderResponseDto>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllOrdersAsync();
            return orders.Select(MapToDto).ToList();
        }

        public async Task<List<OrderResponseDto>> GetAllOrdersByStatusAsync(OrderStatus? status = null)
        {
            var orders = await _orderRepository.GetAllOrdersByStatusAsync(status);
            return orders.Select(MapToDto).ToList();
        }

        public async Task<bool> PatchOrderStatusAsync(int orderId, UpdateOrderStatusDto dto)
        {
            if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var parsedStatus))
                throw new Exception("Invalid order status. Allowed values: Pending, Completed, Cancelled.");

            
            var order = await _orderRepository.GetByIdAsync(orderId);

            if (order == null)
                return false;

            if (!Enum.IsDefined(typeof(OrderStatus), dto.Status))
                throw new ArgumentException("Invalid order status.");

            order.Status = parsedStatus;
            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static OrderResponseDto MapToDto(Order o)
        {
            return new OrderResponseDto
            {
                OrderId = o.Id,
                CreatedAt = o.CreatedAt,
                TotalPrice = o.TotalAmount,
                Status = o.Status.ToString(),
                Items = o.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    ProductId = oi.ProductId,
                    //ProductName = oi.Product?.Name ?? string.Empty,
                    Price = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    TotalPrice = oi.UnitPrice * oi.Quantity
                }).ToList()
            };
        }

        public async Task<IEnumerable<object>> GetAllOrdersForAdminAsync()
        {
            var orders = await _orderRepository.GetAllOrdersForAdminAsync();

            return orders.Select(o => new
            {
                orderId = o.Id,
                createdAt = o.CreatedAt,
                status = o.Status.ToString(),
                //paymentStatus = o.PaymentStatus.ToString(),
                //paymentMethod = o.PaymentMethod.ToString(),
                totalPrice = o.TotalAmount,
                recipientName = o.RecipientName,
                items = o.OrderItems.Select(i => new
                {
                    //productName = i.Product.Name,
                    quantity = i.Quantity,
                    totalPrice = i.Order.TotalAmount
                }).ToList()
            });
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto dto)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return false;

            if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var parsedStatus))
                throw new Exception("Invalid order status. Allowed values: Pending, Completed, Cancelled.");

            order.Status = parsedStatus;

            await _orderRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePaymentStatusAsync(int orderId, UpdatePaymentStatusDto dto)
        {
            //var order = await _orderRepository.GetByIdAsync(orderId);
            //if (order == null) return false;

            //if (!Enum.TryParse<PaymentStatus>(dto.PaymentStatus, true, out var parsedPaymentStatus))
            //    throw new Exception("Invalid payment status. Allowed values: Pending, Paid, Failed.");

            //order.PaymentStatus = parsedPaymentStatus;

            //await _orderRepository.SaveChangesAsync();
            return true;
        }
    }
}