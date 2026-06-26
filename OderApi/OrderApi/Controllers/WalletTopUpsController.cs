using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderApi.DTOs.WalletTopUps;
using OrderApi.Services;

namespace OrderApi.Controllers
{
    [ApiController]
    [Route("api/WalletTopUps")]
    public class WalletTopUpsController : ControllerBase
    {
        private readonly IWalletTopUpService _walletTopUpService;

        public WalletTopUpsController(IWalletTopUpService walletTopUpService)
        {
            _walletTopUpService = walletTopUpService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            var requests = await _walletTopUpService.GetAllAsync(status);
            return Ok(requests);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _walletTopUpService.GetByIdAsync(id);
            if (request == null) return NotFound();
            return Ok(request);
        }

        [HttpGet("customer/{customerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            var requests = await _walletTopUpService.GetByCustomerIdAsync(customerId);
            return Ok(requests);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] CreateWalletTopUpRequestDto dto)
        {
            try
            {
                var request = await _walletTopUpService.CreateAsync(dto);
                return Ok(request);
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

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> Approve(int id, [FromBody] ReviewWalletTopUpRequestDto dto)
        {
            try
            {
                var request = await _walletTopUpService.ApproveAsync(id, dto);
                return Ok(request);
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

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> Reject(int id, [FromBody] ReviewWalletTopUpRequestDto dto)
        {
            try
            {
                var request = await _walletTopUpService.RejectAsync(id, dto);
                return Ok(request);
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
