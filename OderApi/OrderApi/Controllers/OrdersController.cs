using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderApi.DTOs.Orders;
using OrderApi.Services;
using System.Security.Claims;

namespace OrderApi.Controllers
{
    [ApiController]
    [Route("api/Orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search = null,
            [FromQuery] int? customerId = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            if (customerId.HasValue)
            {
                var customerOrders = await _orderService.GetOrdersByCustomerIdAsync(
                    customerId.Value,
                    search,
                    status,
                    fromDate,
                    toDate);
                return Ok(customerOrders);
            }

            var user = HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
                return Unauthorized(new { message = "Cần đăng nhập nhân viên để xem toàn bộ đơn hàng." });

            if (!user.IsInRole("Admin") && !user.IsInRole("Sales") && !user.IsInRole("Warehouse"))
                return Forbid();

            var orders = await _orderService.GetAllOrdersAsync(search, status, fromDate, toDate);
            return Ok(orders);
        }

        [HttpGet("lookup")]
        [AllowAnonymous]
        public async Task<IActionResult> Lookup([FromQuery] string? orderCode, [FromQuery] string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest(new { message = "Vui lòng nhập số điện thoại." });

            // Phone-only mode: return list of all orders for this phone
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                var orders = await _orderService.LookupByPhoneAsync(phone);
                return Ok(orders);
            }

            // Code + phone mode: return single matching order
            var order = await _orderService.LookupOrderAsync(orderCode, phone);
            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn hàng phù hợp. Kiểm tra lại mã đơn và số điện thoại." });

            return Ok(order);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            try
            {
                if (dto.CreatedByUserId <= 0)
                    dto.CreatedByUserId = TryGetCurrentUserId(User) ?? 1;

                var order = await _orderService.CreateOrderAsync(dto);
                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static int? TryGetCurrentUserId(ClaimsPrincipal user)
        {
            var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub")
                ?? user.FindFirstValue("userId");

            return int.TryParse(raw, out var userId) ? userId : null;
        }

        private static string? TryGetCurrentUserName(ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Name)
                ?? user.FindFirstValue("name")
                ?? user.FindFirstValue("username")
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // Endpoint cho Machine4 (KhoPro) gọi với order code string thay vì int id
        [HttpPut("by-code/{code}/status")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateStatusByCode(string code, [FromBody] UpdateOrderStatusRequest request)
        {
            var order = await _orderService.GetOrderByCodeAsync(code.Trim().ToUpperInvariant());
            if (order == null)
                return NotFound(new { message = $"Không tìm thấy đơn hàng '{code}'." });

            try
            {
                var approvedBy = request.ApprovedBy ?? TryGetCurrentUserName(User) ?? "KhoPro";
                var updated = await _orderService.UpdateOrderStatusAsync(order.OrderId, request.Status, approvedBy);
                return Ok(updated);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Sales,Warehouse")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var approvedBy = TryGetCurrentUserName(User) ?? request.ApprovedBy;
                var order = await _orderService.UpdateOrderStatusAsync(id, request.Status, approvedBy);
                return Ok(order);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var ok = await _orderService.CancelOrderAsync(id);
                if (!ok) return NotFound();
                return Ok(new { message = "Đơn hàng đã được hủy" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/customer-cancel")]
        [AllowAnonymous]
        public async Task<IActionResult> CustomerCancel(int id, [FromBody] CustomerCancelOrderRequest request)
        {
            try
            {
                var ok = await _orderService.CancelOrderForCustomerAsync(id, request.Phone);
                if (!ok) return NotFound(new { message = "Không tìm thấy đơn hàng phù hợp với số điện thoại." });
                return Ok(new { message = "Đơn hàng đã được hủy" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _orderService.DeleteOrderAsync(id);
            if (!ok) return NotFound();
            return Ok();
        }
    }

    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = "";
        public string? ApprovedBy { get; set; }
    }

    public class CustomerCancelOrderRequest
    {
        public string Phone { get; set; } = "";
    }
}
