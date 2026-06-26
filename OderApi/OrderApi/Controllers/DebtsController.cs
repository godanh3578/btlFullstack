using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderApi.DTOs.Debts;
using OrderApi.Services;

namespace OrderApi.Controllers
{
    [ApiController]
    [Route("api/Debts")]
    [Authorize(Roles = "Admin,Sales")]
    public class DebtsController : ControllerBase
    {
        private readonly IDebtService _debtService;

        public DebtsController(IDebtService debtService)
        {
            _debtService = debtService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var debts = await _debtService.GetAllDebtsAsync();
            return Ok(debts);
        }

        [HttpGet("report")]
        public async Task<IActionResult> GetReport()
        {
            var report = await _debtService.GetDebtReportAsync();
            return Ok(report);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var debt = await _debtService.GetDebtByIdAsync(id);
            if (debt == null) return NotFound();
            return Ok(debt);
        }

        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            try
            {
                var result = await _debtService.GetDebtsByCustomerIdAsync(customerId);
                return Ok(result);
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

        [HttpPost("{id}/pay")]
        public async Task<IActionResult> Pay(int id, [FromBody] CreateDebtPaymentDto dto)
        {
            try
            {
                var debt = await _debtService.PayDebtAsync(id, dto);
                return Ok(new { message = "Payment recorded", debt });
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
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateDebtStatusDto dto)
        {
            try
            {
                var debt = await _debtService.UpdateDebtStatusAsync(id, dto);
                return Ok(debt);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
