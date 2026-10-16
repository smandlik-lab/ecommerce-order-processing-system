using EcommerceOrderProcessing.DTOs.OrderDTOs;
using EcommerceOrderProcessing.Models;
using EcommerceOrderProcessing.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
//using Swashbuckle.AspNetCore.Filters;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace EcommerceOrderProcessing.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult> CreateOrder()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                _logger.LogInformation("Create order request received for UserId: {UserId}", userId);
                await _orderService.CreateOrderAsync(userId);
                _logger.LogInformation("Order created successfully for UserId: {UserId}", userId);
                return Ok("Order created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating order for UserId: {UserId}. Details: {ErrorMessage}", userId, ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("my-orders")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetMyOrders()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                _logger.LogInformation("GetMyOrders request received for UserId: {UserId}", userId);
                var orders = await _orderService.GetMyOrdersAsync(userId);
                _logger.LogInformation("Retrieved {OrderCount} orders for UserId: {UserId}", orders.Count, userId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving orders for UserId: {UserId}. Details: {ErrorMessage}", userId, ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]        
        public async Task<ActionResult<OrderResponseDto>> GetOrderById(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                _logger.LogInformation("GetOrderById request received for OrderId: {OrderId}, UserId: {UserId}", id, userId);
                var order = await _orderService.GetOrderByIdAsync(userId, id);

                if (order == null)
                {
                    _logger.LogWarning("Order not found. OrderId: {OrderId}, UserId: {UserId}", id, userId);
                    return NotFound("Order not found.");
                }

                _logger.LogInformation("Order retrieved successfully. OrderId: {OrderId}, UserId: {UserId}", id, userId);
                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving order. OrderId: {OrderId}, UserId: {UserId}. Details: {ErrorMessage}", id, userId, ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("cancel/{id}")]
        public async Task<ActionResult> CancelOrder(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                _logger.LogInformation("Cancel order request received for OrderId: {OrderId}, UserId: {UserId}", id, userId);
                var result = await _orderService.CancelOrderAsync(userId, id);
                _logger.LogInformation("Order cancelled successfully. OrderId: {OrderId}, UserId: {UserId}", id, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cancelling order. OrderId: {OrderId}, UserId: {UserId}. Details: {ErrorMessage}", id, userId, ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("all")]
        //[Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetAllOrders()
        {
            try
            {
                _logger.LogInformation("GetAllOrders request received");
                var orders = await _orderService.GetAllOrdersAsync();
                _logger.LogInformation("Retrieved {OrderCount} orders", orders.Count);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all orders. Details: {ErrorMessage}", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("all/filter")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetAllOrdersWithStatusFilter([FromQuery] string? status = null)
        {
            try
            {
                _logger.LogInformation("GetAllOrdersWithStatusFilter request received with status: {Status}", status ?? "null");
                OrderStatus? parsedStatus = null;

                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (!Enum.TryParse<OrderStatus>(status, true, out var statusValue))
                    {
                        _logger.LogWarning("Invalid status filter provided: {Status}", status);
                        return BadRequest(new { message = "Invalid status. Allowed values: Pending, Completed, Cancelled." });
                    }

                    parsedStatus = statusValue;
                }

                var orders = await _orderService.GetAllOrdersByStatusAsync(parsedStatus);
                _logger.LogInformation("Retrieved {OrderCount} orders with status filter: {Status}", orders.Count, status ?? "null");
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving orders with status filter. Details: {ErrorMessage}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> PatchOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (dto == null)
            {
                _logger.LogWarning("PatchOrderStatus failed: Invalid data for OrderId: {OrderId}", id);
                return BadRequest("Invalid data.");
            }

            try
            {
                _logger.LogInformation("PatchOrderStatus request received for OrderId: {OrderId}, NewStatus: {NewStatus}", id, dto.Status);
                var result = await _orderService.PatchOrderStatusAsync(id, dto);

                if (!result)
                {
                    _logger.LogWarning("Order not found while updating status. OrderId: {OrderId}", id);
                    return NotFound("Order not found.");
                }

                _logger.LogInformation("Order status updated successfully. OrderId: {OrderId}, NewStatus: {NewStatus}", id, dto.Status);
                return Ok(new { message = "Order status updated successfully." });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument error while updating order status. OrderId: {OrderId}. Details: {ErrorMessage}", id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating order status. OrderId: {OrderId}. Details: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, new { message = "An error occurred while updating order status." });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/payment-status")]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusDto dto)
        {
            try
            {
                _logger.LogInformation("UpdatePaymentStatus request received for OrderId: {OrderId}", id);
                var updated = await _orderService.UpdatePaymentStatusAsync(id, dto);

                if (!updated)
                {
                    _logger.LogWarning("Order not found while updating payment status. OrderId: {OrderId}", id);
                    return NotFound("Order not found.");
                }

                _logger.LogInformation("Payment status updated successfully for OrderId: {OrderId}", id);
                return Ok("Payment status updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating payment status. OrderId: {OrderId}. Details: {ErrorMessage}", id, ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }
}