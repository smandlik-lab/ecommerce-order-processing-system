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
        private readonly IOrderQueueService _orderQueueService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IOrderRepository orderRepository, IUnitOfWork unitOfWork, IOrderQueueService orderQueueService, ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _orderQueueService = orderQueueService;
            _logger = logger;
        }

        public async Task CreateOrderAsync(int userId)
        {
            try
            {
                _logger.LogInformation("CreateOrder process started for UserId: {UserId}", userId);
                var cart = await _orderRepository.GetCartWithItemsAndProductsByUserIdAsync(userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    _logger.LogWarning("CreateOrder failed: Cart is empty for UserId: {UserId}", userId);
                    throw new Exception("Cart is empty.");
                }

                foreach (var item in cart.CartItems)
                {
                    if (item.Product == null)
                    {
                        _logger.LogError("CreateOrder failed: Invalid product in cart for UserId: {UserId}", userId);
                        throw new Exception("Invalid product in cart.");
                    }

                    if (item.Quantity > item.Product.StockQuantity)
                    {
                        _logger.LogWarning("CreateOrder failed: Not enough stock for product: {ProductName}, UserId: {UserId}", item.Product.Name, userId);
                        throw new Exception($"Not enough stock for product: {item.Product.Name}");
                    }
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

                // Enqueue pending order for processing with 5-minute delay
                await _orderQueueService.EnqueuePendingOrderAsync(order.Id);

                _logger.LogInformation("Order created successfully. OrderId: {OrderId}, UserId: {UserId}, TotalAmount: {TotalAmount}", order.Id, userId, totalPrice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during CreateOrder for UserId: {UserId}. Details: {ErrorMessage}", userId, ex.Message);
                throw;
            }
        }

        public async Task<List<OrderResponseDto>> GetMyOrdersAsync(int userId)
        {
            try
            {
                _logger.LogInformation("GetMyOrders process started for UserId: {UserId}", userId);
                var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);
                _logger.LogInformation("Retrieved {OrderCount} orders for UserId: {UserId}", orders.Count, userId);
                return orders.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during GetMyOrders for UserId: {UserId}. Details: {ErrorMessage}", userId, ex.Message);
                throw;
            }
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int userId, int orderId)
        {
            try
            {
                _logger.LogInformation("GetOrderById process started for OrderId: {OrderId}, UserId: {UserId}", orderId, userId);
                var order = await _orderRepository.GetOrderByIdAsync(orderId);

                if (order == null || order.UserId != userId)
                {
                    _logger.LogWarning("Order not found or unauthorized access. OrderId: {OrderId}, UserId: {UserId}", orderId, userId);
                    return null;
                }

                _logger.LogInformation("Order retrieved successfully. OrderId: {OrderId}, UserId: {UserId}", orderId, userId);
                return MapToDto(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during GetOrderById. OrderId: {OrderId}, UserId: {UserId}. Details: {ErrorMessage}", orderId, userId, ex.Message);
                throw;
            }
        }

        public async Task<object> CancelOrderAsync(int userId, int orderId)
        {
            try
            {
                _logger.LogInformation("CancelOrder process started for OrderId: {OrderId}, UserId: {UserId}", orderId, userId);
                var order = await _orderRepository.GetByIdWithItemsAndProductsAsync(orderId);

                if (order == null || order.UserId != userId)
                {
                    _logger.LogWarning("Order not found or unauthorized access. OrderId: {OrderId}, UserId: {UserId}", orderId, userId);
                    throw new Exception("Order not found.");
                }

                if (order.Status == OrderStatus.Completed)
                {
                    _logger.LogWarning("Cannot cancel completed order. OrderId: {OrderId}, UserId: {UserId}", orderId, userId);
                    throw new Exception("Completed orders cannot be cancelled.");
                }

                if (order.Status == OrderStatus.Cancelled)
                {
                    _logger.LogWarning("Order is already cancelled. OrderId: {OrderId}, UserId: {UserId}", orderId, userId);
                    throw new Exception("Order is already cancelled.");
                }

                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null)
                        item.Product.StockQuantity += item.Quantity;
                }

                order.Status = OrderStatus.Cancelled;
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Order cancelled successfully. OrderId: {OrderId}, UserId: {UserId}", orderId, userId);

                return new
                {
                    message = "Order cancelled successfully",
                    orderId = order.Id,
                    status = order.Status.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during CancelOrder. OrderId: {OrderId}, UserId: {UserId}. Details: {ErrorMessage}", orderId, userId, ex.Message);
                throw;
            }
        }

        public async Task<List<OrderResponseDto>> GetAllOrdersAsync()
        {
            try
            {
                _logger.LogInformation("GetAllOrders process started");
                var orders = await _orderRepository.GetAllOrdersAsync();
                _logger.LogInformation("Retrieved {OrderCount} orders", orders.Count);
                return orders.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during GetAllOrders. Details: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<List<OrderResponseDto>> GetAllOrdersByStatusAsync(OrderStatus? status = null)
        {
            try
            {
                _logger.LogInformation("GetAllOrdersByStatus process started with status: {Status}", status?.ToString() ?? "null");
                var orders = await _orderRepository.GetAllOrdersByStatusAsync(status);
                _logger.LogInformation("Retrieved {OrderCount} orders with status: {Status}", orders.Count, status?.ToString() ?? "null");
                return orders.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during GetAllOrdersByStatus with status: {Status}. Details: {ErrorMessage}", status?.ToString() ?? "null", ex.Message);
                throw;
            }
        }

        public async Task<bool> PatchOrderStatusAsync(int orderId, UpdateOrderStatusDto dto)
        {
            try
            {
                _logger.LogInformation("PatchOrderStatus process started for OrderId: {OrderId}, NewStatus: {NewStatus}", orderId, dto.Status);

                if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var parsedStatus))
                {
                    _logger.LogWarning("Invalid order status provided. OrderId: {OrderId}, ProvidedStatus: {ProvidedStatus}", orderId, dto.Status);
                    throw new Exception("Invalid order status. Allowed values: Pending, Completed, Cancelled.");
                }

                var order = await _orderRepository.GetByIdAsync(orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order not found for status update. OrderId: {OrderId}", orderId);
                    return false;
                }

                if (!Enum.IsDefined(typeof(OrderStatus), dto.Status))
                {
                    _logger.LogWarning("Invalid order status for update. OrderId: {OrderId}, Status: {Status}", orderId, dto.Status);
                    throw new ArgumentException("Invalid order status.");
                }

                order.Status = parsedStatus;
                _orderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Order status updated successfully. OrderId: {OrderId}, NewStatus: {NewStatus}", orderId, parsedStatus);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during PatchOrderStatus. OrderId: {OrderId}. Details: {ErrorMessage}", orderId, ex.Message);
                throw;
            }
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
            try
            {
                _logger.LogInformation("GetAllOrdersForAdmin process started");
                var orders = await _orderRepository.GetAllOrdersForAdminAsync();

                var result = orders.Select(o => new
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
                        totalPrice = i.Order?.TotalAmount
                    }).ToList()
                });

                _logger.LogInformation("Retrieved {OrderCount} orders for admin", orders.Count());
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during GetAllOrdersForAdmin. Details: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto dto)
        {
            try
            {
                _logger.LogInformation("UpdateOrderStatus process started for OrderId: {OrderId}, NewStatus: {NewStatus}", orderId, dto.Status);
                var order = await _orderRepository.GetByIdAsync(orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order not found for status update. OrderId: {OrderId}", orderId);
                    return false;
                }

                if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var parsedStatus))
                {
                    _logger.LogWarning("Invalid payment status provided. OrderId: {OrderId}, ProvidedStatus: {ProvidedStatus}", orderId, dto.Status);
                    throw new Exception("Invalid order status. Allowed values: Pending, Completed, Cancelled.");
                }

                order.Status = parsedStatus;

                await _orderRepository.SaveChangesAsync();
                _logger.LogInformation("Order status updated successfully. OrderId: {OrderId}, NewStatus: {NewStatus}", orderId, parsedStatus);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during UpdateOrderStatus. OrderId: {OrderId}. Details: {ErrorMessage}", orderId, ex.Message);
                throw;
            }
        }

        public async Task<bool> UpdatePaymentStatusAsync(int orderId, UpdatePaymentStatusDto dto)
        {
            try
            {
                _logger.LogInformation("UpdatePaymentStatus process started for OrderId: {OrderId}", orderId);
                //var order = await _orderRepository.GetByIdAsync(orderId);
                //if (order == null) return false;

                //if (!Enum.TryParse<PaymentStatus>(dto.PaymentStatus, true, out var parsedPaymentStatus))
                //    throw new Exception("Invalid payment status. Allowed values: Pending, Paid, Failed.");

                //order.PaymentStatus = parsedPaymentStatus;

                //await _orderRepository.SaveChangesAsync();
                _logger.LogInformation("UpdatePaymentStatus completed for OrderId: {OrderId}", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during UpdatePaymentStatus. OrderId: {OrderId}. Details: {ErrorMessage}", orderId, ex.Message);
                throw;
            }
        }
    }
}