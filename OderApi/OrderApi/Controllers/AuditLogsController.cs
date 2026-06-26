using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Models;

namespace OrderApi.Controllers
{
    [ApiController]
    [Route("api/AuditLogs")]
    [Authorize(Roles = "Admin,Sales")]
    public class AuditLogsController : ControllerBase
    {
        private readonly OrderDbContext _context;

        public AuditLogsController(OrderDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int limit = 80)
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(log => log.PerformedAt)
                .Take(Math.Clamp(limit, 1, 200))
                .Select(log => new
                {
                    log.AuditLogId,
                    log.Action,
                    log.EntityName,
                    log.EntityId,
                    log.OldValue,
                    log.NewValue,
                    log.PerformedBy,
                    log.PerformedAt,
                    log.IpAddress
                })
                .ToListAsync();

            return Ok(logs);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAuditLogRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Action) || string.IsNullOrWhiteSpace(request.EntityName))
                return BadRequest(new { message = "Action and entityName are required." });

            var log = new AuditLog
            {
                Action = request.Action.Trim(),
                EntityName = request.EntityName.Trim(),
                EntityId = string.IsNullOrWhiteSpace(request.EntityId) ? "-" : request.EntityId.Trim(),
                OldValue = request.OldValue,
                NewValue = request.NewValue,
                PerformedBy = string.IsNullOrWhiteSpace(request.PerformedBy)
                    ? User.Identity?.Name ?? "staff"
                    : request.PerformedBy.Trim(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                PerformedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(log);
        }
    }

    public class CreateAuditLogRequest
    {
        public string Action { get; set; } = "";
        public string EntityName { get; set; } = "";
        public string EntityId { get; set; } = "";
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string PerformedBy { get; set; } = "";
    }
}
