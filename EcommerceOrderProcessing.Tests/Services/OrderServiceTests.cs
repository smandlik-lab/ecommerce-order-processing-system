using Moq;
using Xunit;
using FluentAssertions;
using EcommerceOrderProcessing.DTOs.OrderDTOs;
using EcommerceOrderProcessing.Models;
using EcommerceOrderProcessing.Services.Implementations;
using EcommerceOrderProcessing.Services.Interfaces;
using EcommerceOrderProcessing.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Amazon.SQS.Model;

namespace EcommerceOrderProcessing.Tests.Services
{
    public class OrderServiceTests : BaseServiceTest
    {
        private readonly OrderService _orderService;
        private readonly Mock<IOrderQueueService> _mockOrderQueueService;
        private readonly Mock<ILogger<OrderService>> _mockLogger;

        public OrderServiceTests()
        {
            _mockOrderQueueService = new Mock<IOrderQueueService>();
            _mockLogger = CreateMockLogger<OrderService>();

            _orderService = new OrderService(
                MockOrderRepository.Object,
                MockUnitOfWork.Object,
                _mockOrderQueueService.Object,
                _mockLogger.Object
            );
        }

        #region CreateOrderAsync Tests

        [Fact]
        public async Task CreateOrderAsync_WithValidCart_ShouldCreateOrderAndEnqueueToSqs()
        {
            // Arrange
            int userId = 1;
            var cart = TestDataFactory.CreateTestCart(userId);
            var expectedOrder = TestDataFactory.CreateTestOrder(1, userId, OrderStatus.Pending);

            MockOrderRepository.Setup(r => r.GetCartWithItemsAndProductsByUserIdAsync(userId))
                .ReturnsAsync(cart);
            MockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);
            MockOrderRepository.Setup(r => r.RemoveCartItemsRange(It.IsAny<IEnumerable<CartItem>>()));
            MockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockOrderQueueService.Setup(q => q.EnqueuePendingOrderAsync(It.IsAny<int>(), 300))
                .Returns(Task.CompletedTask);

            // Act
            await _orderService.CreateOrderAsync(userId);

            // Assert
            MockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _mockOrderQueueService.Verify(q => q.EnqueuePendingOrderAsync(It.IsAny<int>(), 300), Times.Once);
        }

        [Fact]
        public async Task CreateOrderAsync_WithEmptyCart_ShouldThrowException()
        {
            // Arrange
            int userId = 1;
            var emptyCart = new Cart { Id = 1, UserId = userId, CartItems = new List<CartItem>() };

            MockOrderRepository.Setup(r => r.GetCartWithItemsAndProductsByUserIdAsync(userId))
                .ReturnsAsync(emptyCart);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _orderService.CreateOrderAsync(userId));
            _mockOrderQueueService.Verify(q => q.EnqueuePendingOrderAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CreateOrderAsync_WithNullCart_ShouldThrowException()
        {
            // Arrange
            int userId = 1;
            MockOrderRepository.Setup(r => r.GetCartWithItemsAndProductsByUserIdAsync(userId))
                .ReturnsAsync((Cart)null!);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _orderService.CreateOrderAsync(userId));
            _mockOrderQueueService.Verify(q => q.EnqueuePendingOrderAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CreateOrderAsync_WithInsufficientStock_ShouldThrowException()
        {
            // Arrange
            int userId = 1;
            var product = TestDataFactory.CreateTestProduct(1, "Limited Product");
            product.StockQuantity = 1; // Only 1 in stock

            var cart = new Cart
            {
                Id = 1,
                UserId = userId,
                CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        Id = 1,
                        CartId = 1,
                        ProductId = 1,
                        Quantity = 5, // Trying to buy 5
                        Product = product
                    }
                }
            };

            MockOrderRepository.Setup(r => r.GetCartWithItemsAndProductsByUserIdAsync(userId))
                .ReturnsAsync(cart);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _orderService.CreateOrderAsync(userId));
            _mockOrderQueueService.Verify(q => q.EnqueuePendingOrderAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CreateOrderAsync_WithMultipleCartItems_ShouldCalculateTotalAmountCorrectly()
        {
            // Arrange
            int userId = 1;
            var cart = TestDataFactory.CreateCartWithMultipleItems(userId, 3);
            // Total should be: (1*1 + 2*2 + 3*3) * 49.99 = (1+4+9) * 49.99 = 14 * 49.99 = 699.86

            MockOrderRepository.Setup(r => r.GetCartWithItemsAndProductsByUserIdAsync(userId))
                .ReturnsAsync(cart);
            MockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);
            MockOrderRepository.Setup(r => r.RemoveCartItemsRange(It.IsAny<IEnumerable<CartItem>>()));
            MockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockOrderQueueService.Setup(q => q.EnqueuePendingOrderAsync(It.IsAny<int>(), 300))
                .Returns(Task.CompletedTask);

            // Act
            await _orderService.CreateOrderAsync(userId);

            // Assert
            MockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o =>
                o.TotalAmount == 699.86m &&
                o.Status == OrderStatus.Pending &&
                o.OrderItems.Count == 3
            )), Times.Once);
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldReduceProductStockQuantity()
        {
            // Arrange
            int userId = 1;
            var product = TestDataFactory.CreateTestProduct(1, "Product");
            product.StockQuantity = 100;

            var cart = new Cart
            {
                Id = 1,
                UserId = userId,
                CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        Id = 1,
                        CartId = 1,
                        ProductId = 1,
                        Quantity = 10,
                        Product = product
                    }
                }
            };

            MockOrderRepository.Setup(r => r.GetCartWithItemsAndProductsByUserIdAsync(userId))
                .ReturnsAsync(cart);
            MockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);
            MockOrderRepository.Setup(r => r.RemoveCartItemsRange(It.IsAny<IEnumerable<CartItem>>()));
            MockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockOrderQueueService.Setup(q => q.EnqueuePendingOrderAsync(It.IsAny<int>(), 300))
                .Returns(Task.CompletedTask);

            // Act
            await _orderService.CreateOrderAsync(userId);

            // Assert
            product.StockQuantity.Should().Be(90); // 100 - 10
        }

        #endregion

        #region GetMyOrdersAsync Tests

        [Fact]
        public async Task GetMyOrdersAsync_WithValidUserId_ShouldReturnOrders()
        {
            // Arrange
            int userId = 1;
            var orders = new List<Order>
            {
                TestDataFactory.CreateTestOrder(1, userId, OrderStatus.Pending),
                TestDataFactory.CreateTestOrder(2, userId, OrderStatus.Completed)
            };

            MockOrderRepository.Setup(r => r.GetOrdersByUserIdAsync(userId))
                .ReturnsAsync(orders);

            // Act
            var result = await _orderService.GetMyOrdersAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            MockOrderRepository.Verify(r => r.GetOrdersByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetMyOrdersAsync_WithNoOrders_ShouldReturnEmptyList()
        {
            // Arrange
            int userId = 1;
            MockOrderRepository.Setup(r => r.GetOrdersByUserIdAsync(userId))
                .ReturnsAsync(new List<Order>());

            // Act
            var result = await _orderService.GetMyOrdersAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region GetOrderByIdAsync Tests

        [Fact]
        public async Task GetOrderByIdAsync_WithValidOrderAndUser_ShouldReturnOrder()
        {
            // Arrange
            int userId = 1;
            int orderId = 1;
            var order = TestDataFactory.CreateTestOrder(orderId, userId);

            MockOrderRepository.Setup(r => r.GetOrderByIdAsync(orderId))
                .ReturnsAsync(order);

            // Act
            var result = await _orderService.GetOrderByIdAsync(userId, orderId);

            // Assert
            result.Should().NotBeNull();
            MockOrderRepository.Verify(r => r.GetOrderByIdAsync(orderId), Times.Once);
        }

        [Fact]
        public async Task GetOrderByIdAsync_WithUnauthorizedUser_ShouldReturnNull()
        {
            // Arrange
            int userId = 1;
            int orderId = 1;
            int differentUserId = 2;
            var order = TestDataFactory.CreateTestOrder(orderId, differentUserId);

            MockOrderRepository.Setup(r => r.GetOrderByIdAsync(orderId))
                .ReturnsAsync(order);

            // Act
            var result = await _orderService.GetOrderByIdAsync(userId, orderId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetOrderByIdAsync_WithNonexistentOrder_ShouldReturnNull()
        {
            // Arrange
            int userId = 1;
            int orderId = 999;

            MockOrderRepository.Setup(r => r.GetOrderByIdAsync(orderId))
                .ReturnsAsync((Order)null!);

            // Act
            var result = await _orderService.GetOrderByIdAsync(userId, orderId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CancelOrderAsync Tests

        [Fact]
        public async Task CancelOrderAsync_WithPendingOrder_ShouldCancelSuccessfully()
        {
            // Arrange
            int userId = 1;
            int orderId = 1;
            var order = TestDataFactory.CreateTestOrder(orderId, userId, OrderStatus.Pending);

            MockOrderRepository.Setup(r => r.GetByIdWithItemsAndProductsAsync(orderId))
                .ReturnsAsync(order);
            MockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _orderService.CancelOrderAsync(userId, orderId);

            // Assert
            result.Should().NotBeNull();
            order.Status.Should().Be(OrderStatus.Cancelled);
            MockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelOrderAsync_WithCompletedOrder_ShouldThrowException()
        {
            // Arrange
            int userId = 1;
            int orderId = 1;
            var order = TestDataFactory.CreateTestOrder(orderId, userId, OrderStatus.Completed);

            MockOrderRepository.Setup(r => r.GetByIdWithItemsAndProductsAsync(orderId))
                .ReturnsAsync(order);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _orderService.CancelOrderAsync(userId, orderId));
        }

        [Fact]
        public async Task CancelOrderAsync_WithCancelledOrder_ShouldThrowException()
        {
            // Arrange
            int userId = 1;
            int orderId = 1;
            var order = TestDataFactory.CreateTestOrder(orderId, userId, OrderStatus.Cancelled);

            MockOrderRepository.Setup(r => r.GetByIdWithItemsAndProductsAsync(orderId))
                .ReturnsAsync(order);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _orderService.CancelOrderAsync(userId, orderId));
        }

        [Fact]
        public async Task CancelOrderAsync_WithUnauthorizedUser_ShouldThrowException()
        {
            // Arrange
            int userId = 1;
            int orderId = 1;
            int differentUserId = 2;
            var order = TestDataFactory.CreateTestOrder(orderId, differentUserId, OrderStatus.Pending);

            MockOrderRepository.Setup(r => r.GetByIdWithItemsAndProductsAsync(orderId))
                .ReturnsAsync(order);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _orderService.CancelOrderAsync(userId, orderId));
        }

        #endregion
    }
}
