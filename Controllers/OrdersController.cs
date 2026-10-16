using EcommerceOrderProcessing.DTOs.OrderDTOs;
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

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<ActionResult> CreateOrder()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                await _orderService.CreateOrderAsync(userId);
                return Ok("Order created successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("my-orders")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetMyOrders()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var orders = await _orderService.GetMyOrdersAsync(userId);
            return Ok(orders);
        }

        [HttpGet("{id}")]        
        public async Task<ActionResult<OrderResponseDto>> GetOrderById(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var order = await _orderService.GetOrderByIdAsync(userId, id);

            if (order == null)
                return NotFound("Order not found.");

            return Ok(order);
        }

        [HttpPut("cancel/{id}")]
        public async Task<ActionResult> CancelOrder(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                var result = await _orderService.CancelOrderAsync(userId, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("all")]
        //[Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> PatchOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid data.");

            try
            {
                var result = await _orderService.PatchOrderStatusAsync(id, dto);

                if (!result)
                    return NotFound("Order not found.");

                return Ok(new { message = "Order status updated successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/payment-status")]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusDto dto)
        {
            try
            {
                var updated = await _orderService.UpdatePaymentStatusAsync(id, dto);

                if (!updated)
                    return NotFound("Order not found.");

                return Ok("Payment status updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}