using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.DTOs.Customers;
using OrderApi.Services;

namespace OrderApi.Controllers
{
    [ApiController]
    [Route("api/Customers")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly OrderDbContext _context;

        public CustomersController(ICustomerService customerService, OrderDbContext context)
        {
            _customerService = customerService;
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> GetAll([FromQuery] string? search)
        {
            var customers = await _customerService.GetAllCustomersAsync();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                customers = customers
                    .Where(c => c.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || c.Phone.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            return Ok(customers);
        }

        [HttpGet("{id}")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();
            return Ok(customer);
        }
        [HttpGet("{id}/profile")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProfile(int id)
        {
    // Hàm này sẽ vào tận DB lôi dữ liệu mới nhất (đã có ngày sinh) ra trả về
            try
            {
                var customerDto = await _customerService.GetCustomerProfileAsync(id);
                return Ok(customerDto);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Tai khoan khong con ton tai." });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }
        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] CustomerLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Phone))
                return BadRequest(new { message = "Vui lòng nhập số điện thoại." });

            var customer = await _customerService.GetCustomerByPhoneAsync(request.Phone.Trim());
            if (customer == null)
                return NotFound(new { message = "Không tìm thấy khách hàng. Vui lòng đăng ký." });

            if (!string.Equals(customer.Status, "Active", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Tai khoan da bi khoa hoac ngung hoat dong." });

            return Ok(customer);
        }

        [HttpGet("exists")]
        [AllowAnonymous]
        public async Task<IActionResult> Exists([FromQuery] string? phone, [FromQuery] string? email)
        {
            var phoneCustomer = string.IsNullOrWhiteSpace(phone)
                ? null
                : await _customerService.GetCustomerByPhoneAsync(phone.Trim());
            var emailCustomer = string.IsNullOrWhiteSpace(email)
                ? null
                : await _customerService.GetCustomerByEmailAsync(email.Trim());

            return Ok(new
            {
                phoneExists = phoneCustomer != null,
                emailExists = emailCustomer != null
            });
        }

        [HttpGet("{id}/purchase-history")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPurchaseHistory(int id)
        {
            try
            {
                var history = await _customerService.GetPurchaseHistoryAsync(id);
                return Ok(history);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/debts")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDebts(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            var debts = await _context.Debts
                .Where(d => d.CustomerId == id)
                .Select(d => new {
                    d.DebtId,
                    d.OrderId,
                    d.DebtAmount,
                    d.PaidAmount,
                    d.RemainingAmount,
                    d.DebtStatus,
                    d.DueDate,
                    d.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                customerId = id,
                customerName = customer.FullName,
                currentDebt = customer.CurrentDebt,
                debts
            });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
        {
            try
            {
                var customer = await _customerService.CreateCustomerAsync(dto);
                return Ok(customer);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.StartsWith("DUPLICATE_", StringComparison.OrdinalIgnoreCase))
                {
                    var message = ex.Message.Contains(':')
                        ? ex.Message[(ex.Message.IndexOf(':') + 1)..].Trim()
                        : ex.Message;
                    return Conflict(new { message });
                }

                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/profile")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateCustomerProfileDto dto)
        {
            try
            {
                var customer = await _customerService.UpdateCustomerProfileAsync(id, dto);
                return Ok(customer);
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
        [HttpPost("{id}/avatar")]
        [AllowAnonymous]
    public async Task<IActionResult> UploadAvatar(int id, IFormFile file)
    {
    if (file == null || file.Length == 0)
        return BadRequest(new { message = "Vui lòng chọn ảnh." });
    var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
    if (!allowed.Contains(file.ContentType))
        return BadRequest(new { message = "Chỉ chấp nhận ảnh JPG, PNG, WEBP." });

    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
    Directory.CreateDirectory(uploadsPath);

    var ext = Path.GetExtension(file.FileName);
    var fileName = $"avatar_{id}{ext}";
    var filePath = Path.Combine(uploadsPath, fileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
        await file.CopyToAsync(stream);

    var avatarUrl = $"/avatars/{fileName}";

    // Lưu URL vào DB
    var customer = await _context.Customers.FindAsync(id);
    if (customer == null) return NotFound();
    customer.AvatarUrl = avatarUrl;
    await _context.SaveChangesAsync();
    return Ok(new { avatarUrl, message = "Ảnh đại diện đã được tải lên thành công." });
}

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Sales")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
        {
            try
            {
                var customer = await _customerService.UpdateCustomerAsync(id, dto);
                return Ok(customer);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ok = await _customerService.DeleteCustomerAsync(id);
                if (!ok) return NotFound();
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.StartsWith("DUPLICATE_", StringComparison.OrdinalIgnoreCase))
                {
                    var message = ex.Message.Contains(':')
                        ? ex.Message[(ex.Message.IndexOf(':') + 1)..].Trim()
                        : ex.Message;
                    return Conflict(new { message });
                }

                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class CustomerLoginRequest
    {
        public string Phone { get; set; } = "";
        public string Password { get; set; } = "";

    }
}
