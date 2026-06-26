using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using OrderApi.Data;



namespace OrderApi.Controllers

{

    [ApiController]

    [Route("api/OutboxMessages")]

    [Authorize(Roles = "Admin,Sales")]

    public class OutboxMessagesController : ControllerBase

    {

        private readonly OrderDbContext _context;



        public OutboxMessagesController(OrderDbContext context)

        {

            _context = context;

        }



        [HttpGet]

        public async Task<IActionResult> GetAll([FromQuery] int limit = 50)

        {

            var messages = await _context.OutboxMessages

                .OrderByDescending(m => m.CreatedAt)

                .Take(limit)

                .Select(m => new

                {

                    m.OutboxMessageId,

                    m.EventName,

                    m.Status,

                    m.RetryCount,

                    m.CreatedAt,

                    m.ProcessedAt

                })

                .ToListAsync();



            return Ok(messages);

        }

    }

}


