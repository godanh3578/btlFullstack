using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderApi.DTOs.Returns;
using OrderApi.Services;

namespace OrderApi.Controllers
{
    [ApiController]
    [Route("api/Returns")]
    public class ReturnsController : ControllerBase
    {
        private readonly IReturnService _returnService;

        public ReturnsController(IReturnService returnService)
        {
            _returnService = returnService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status)
        {
            var list = await _returnService.GetAllAsync(search, status);
            return Ok(list);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> GetById(int id)
        {
            var ret = await _returnService.GetByIdAsync(id);
            if (ret == null) return NotFound();
            return Ok(ret);
        }

        [HttpGet("order/{orderId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var list = await _returnService.GetByOrderIdAsync(orderId);
            return Ok(list);
        }

        [HttpGet("customer/{customerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            var list = await _returnService.GetByCustomerIdAsync(customerId);
            return Ok(list);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> Create([FromBody] CreateReturnDto dto)
        {
            try
            {
                var ret = await _returnService.CreateAsync(dto);
                return Ok(ret);
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

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateReturnStatusDto dto)
        {
            try
            {
                var ret = await _returnService.UpdateStatusAsync(id, dto.Status);
                return Ok(ret);
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
    }
}
