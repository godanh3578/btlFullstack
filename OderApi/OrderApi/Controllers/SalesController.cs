using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using OrderApi.DTOs.Sales;

using OrderApi.Services;

using System.Security.Claims;



namespace OrderApi.Controllers

{

    [ApiController]

    [Route("api/Sales")]

    public class SalesController : ControllerBase

    {

        private readonly ISalesService _salesService;



        public SalesController(ISalesService salesService)

        {

            _salesService = salesService;

        }



        [HttpPost("Checkout")]

        [AllowAnonymous]

        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)

        {

            try

            {

                var createdBy = User.Identity?.IsAuthenticated == true

                    ? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("sub") ?? "sales01"

                    : $"customer-{dto.CustomerId}";



                var result = await _salesService.CheckoutAsync(dto, createdBy);

                return Ok(new

                {

                    success = true,

                    message = "Tạo đơn hàng thành công",

                    data = result

                });

            }

            catch (KeyNotFoundException ex)

            {

                return NotFound(new { success = false, message = ex.Message });

            }

            catch (InvalidOperationException ex)

            {

                return BadRequest(new { success = false, message = ex.Message });

            }

        }



        [HttpPost("calculate-total")]

        [Authorize(Roles = "Admin,Sales")]

        public async Task<IActionResult> CalculateTotal([FromBody] CalculateTotalDto dto)

        {

            try

            {

                var result = await _salesService.CalculateTotalAsync(dto);

                return Ok(new { success = true, data = result });

            }

            catch (InvalidOperationException ex)

            {

                return BadRequest(new { success = false, message = ex.Message });

            }

        }



        [HttpPost("apply-discount")]

        [Authorize(Roles = "Admin,Sales")]

        public async Task<IActionResult> ApplyDiscount([FromBody] CalculateTotalDto dto)

        {

            try

            {

                var result = await _salesService.CalculateTotalAsync(dto);

                return Ok(new { success = true, data = result });

            }

            catch (InvalidOperationException ex)

            {

                return BadRequest(new { success = false, message = ex.Message });

            }

        }

    }

}


