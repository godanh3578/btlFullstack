using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderApi.DTOs.SalesInvoices;
using OrderApi.Services;

namespace OrderApi.Controllers
{
    [ApiController]
    [Route("api/SalesInvoices")]
    public class SalesInvoicesController : ControllerBase
    {
        private readonly ISalesInvoiceService _invoiceService;

        public SalesInvoicesController(ISalesInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? customerId,
            [FromQuery] string? status)
        {
            var list = await _invoiceService.GetAllAsync(customerId, status);
            return Ok(list);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var inv = await _invoiceService.GetByIdAsync(id);
            if (inv == null) return NotFound();
            return Ok(inv);
        }

        [HttpGet("order/{orderId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var inv = await _invoiceService.GetByOrderIdAsync(orderId);
            if (inv == null) return NotFound(new { message = "Chưa có hóa đơn cho đơn hàng này." });
            return Ok(inv);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> Create([FromBody] CreateSalesInvoiceDto dto)
        {
            try
            {
                var inv = await _invoiceService.CreateOrGetAsync(dto);
                return Ok(inv);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
