using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EcommerceOrderProcessing.Controllers;
using EcommerceOrderProcessing.DTOs.OrderDTOs;
using EcommerceOrderProcessing.Models;
using EcommerceOrderProcessing.Services.Interfaces;
using EcommerceOrderProcessing.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace EcommerceOrderProcessing.Tests.Controllers
{
    public class OrdersControllerTests
    {
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly Mock<ILogger<OrdersController>> _mockLogger;
        private readonly OrdersController _controller;
        private readonly int _testUserId = 1;

        public OrdersControllerTests()
        {
            _mockOrderService = new Mock<IOrderService>();
            _mockLogger = new Mock<ILogger<OrdersController>>();

            _controller = new OrdersController(
                _mockOrderService.Object,
                _mockLogger.Object
            );

            // Setup user context
            SetupUserContext(_testUserId);
        }

        private void SetupUserContext(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        #region CreateOrder Tests

        [Fact]
        public async Task CreateOrder_WithValidRequest_ShouldReturnOk()
        {
            // Arrange
            _mockOrderService.Setup(s => s.CreateOrderAsync(_testUserId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateOrder();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.StatusCode.Should().Be(200);
            _mockOrderService.Verify(s => s.CreateOrderAsync(_testUserId), Times.Once);
        }

        [Fact]
        public async Task CreateOrder_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            _mockOrderService.Setup(s => s.CreateOrderAsync(_testUserId))
                .ThrowsAsync(new Exception("Cart is empty."));

            // Act
            var result = await _controller.CreateOrder();

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest?.StatusCode.Should().Be(400);
        }

        #endregion

        #region GetMyOrders Tests

        [Fact]
        public async Task GetMyOrders_WithValidRequest_ShouldReturnOrderList()
        {
            // Arrange
            var orders = new List<OrderResponseDto>
            {
                new OrderResponseDto { OrderId = 1, TotalPrice = 99.99m },
                new OrderResponseDto { OrderId = 2, TotalPrice = 149.99m }
            };

            _mockOrderService.Setup(s => s.GetMyOrdersAsync(_testUserId))
                .ReturnsAsync(orders);

            // Act
            var result = await _controller.GetMyOrders();

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult?.StatusCode.Should().Be(200);
            var returnedOrders = okResult?.Value as IEnumerable<OrderResponseDto>;
            returnedOrders.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetMyOrders_WithNoOrders_ShouldReturnEmptyList()
        {
            // Arrange
            _mockOrderService.Setup(s => s.GetMyOrdersAsync(_testUserId))
                .ReturnsAsync(new List<OrderResponseDto>());

            // Act
            var result = await _controller.GetMyOrders();

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var returnedOrders = okResult?.Value as IEnumerable<OrderResponseDto>;
            returnedOrders.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyOrders_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            _mockOrderService.Setup(s => s.GetMyOrdersAsync(_testUserId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetMyOrders();

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region GetOrderById Tests

        [Fact]
        public async Task GetOrderById_WithValidOrderId_ShouldReturnOrder()
        {
            // Arrange
            var order = new OrderResponseDto
            {
                OrderId = 1,
                TotalPrice = 99.99m,
                Status = nameof(OrderStatus.Pending)
            };

            _mockOrderService.Setup(s => s.GetOrderByIdAsync(_testUserId, 1))
                .ReturnsAsync(order);

            // Act
            var result = await _controller.GetOrderById(1);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult?.StatusCode.Should().Be(200);
            var returnedOrder = okResult?.Value as OrderResponseDto;
            returnedOrder?.OrderId.Should().Be(1);
        }

        [Fact]
        public async Task GetOrderById_WithNonExistentOrder_ShouldReturnNotFound()
        {
            // Arrange
            _mockOrderService.Setup(s => s.GetOrderByIdAsync(_testUserId, 999))
                .ReturnsAsync((OrderResponseDto?)null);

            // Act
            var result = await _controller.GetOrderById(999);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetOrderById_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            _mockOrderService.Setup(s => s.GetOrderByIdAsync(_testUserId, 1))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetOrderById(1);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region CancelOrder Tests

        [Fact]
        public async Task CancelOrder_WithValidOrderId_ShouldReturnOk()
        {
            // Arrange
            var response = new { Success = true, Message = "Order cancelled successfully." };
            _mockOrderService.Setup(s => s.CancelOrderAsync(_testUserId, 1))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.CancelOrder(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task CancelOrder_WithNonExistentOrder_ShouldReturnBadRequest()
        {
            // Arrange
            _mockOrderService.Setup(s => s.CancelOrderAsync(_testUserId, 999))
                .ThrowsAsync(new Exception("Order not found."));

            // Act
            var result = await _controller.CancelOrder(999);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CancelOrder_WithCompletedOrder_ShouldReturnBadRequest()
        {
            // Arrange
            _mockOrderService.Setup(s => s.CancelOrderAsync(_testUserId, 1))
                .ThrowsAsync(new Exception("Completed orders cannot be cancelled."));

            // Act
            var result = await _controller.CancelOrder(1);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CancelOrder_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            _mockOrderService.Setup(s => s.CancelOrderAsync(_testUserId, 1))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.CancelOrder(1);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region PatchOrderStatus Tests

        [Fact]
        public async Task PatchOrderStatus_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var updateDto = TestDataFactory.CreateUpdateOrderStatusDto();
            _mockOrderService.Setup(s => s.PatchOrderStatusAsync(1, updateDto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.PatchOrderStatus(1, updateDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockOrderService.Verify(s => s.PatchOrderStatusAsync(1, updateDto), Times.Once);
        }

        [Fact]
        public async Task PatchOrderStatus_WithInvalidOrderId_ShouldReturnBadRequest()
        {
            // Arrange
            var updateDto = TestDataFactory.CreateUpdateOrderStatusDto();
            _mockOrderService.Setup(s => s.PatchOrderStatusAsync(999, updateDto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.PatchOrderStatus(999, updateDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region GetAllOrders Tests

        [Fact]
        public async Task GetAllOrders_WithManyCalls_ShouldReturnAllOrders()
        {
            // Arrange
            var orders = new List<OrderResponseDto>
            {
                new OrderResponseDto { OrderId = 1, TotalPrice = 99.99m },
                new OrderResponseDto { OrderId = 2, TotalPrice = 149.99m },
                new OrderResponseDto { OrderId = 3, TotalPrice = 199.99m }
            };

            _mockOrderService.Setup(s => s.GetAllOrdersAsync())
                .ReturnsAsync(orders);

            // Act
            var result = await _controller.GetAllOrders();

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var returnedOrders = okResult?.Value as IEnumerable<OrderResponseDto>;
            returnedOrders.Should().HaveCount(3);
        }

        #endregion

        #region GetAllOrdersByStatus Tests

        [Fact]
        public async Task GetAllOrdersByStatus_WithSpecificStatus_ShouldReturnFilteredOrders()
        {
            // Arrange
            var orders = new List<OrderResponseDto>
            {
                new OrderResponseDto { OrderId = 1, TotalPrice = 99.99m, Status = nameof(OrderStatus.Shipped) },
                new OrderResponseDto { OrderId = 2, TotalPrice = 149.99m, Status = nameof(OrderStatus.Shipped) }
            };

            _mockOrderService.Setup(s => s.GetAllOrdersByStatusAsync(OrderStatus.Shipped))
                .ReturnsAsync(orders);

            // Act
            var result = await _controller.GetAllOrdersWithStatusFilter("Shipped");

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var returnedOrders = okResult?.Value as IEnumerable<OrderResponseDto>;
            returnedOrders.Should().HaveCount(2);
        }

        #endregion
    }
}

