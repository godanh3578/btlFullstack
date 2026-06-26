using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using OrderApi.Data;



namespace OrderApi.Controllers

{

    [ApiController]

    [Route("api/ProductStockCaches")]

    public class ProductStockCachesController : ControllerBase

    {

        private readonly OrderDbContext _context;



        public ProductStockCachesController(OrderDbContext context)

        {

            _context = context;

        }



        [HttpGet]

        [AllowAnonymous]

        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? category)

        {

            var query = _context.ProductStockCaches.AsQueryable();



            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("Tất cả", StringComparison.OrdinalIgnoreCase))

            {

                query = query.Where(p => p.CategoryName == category);

            }



            if (!string.IsNullOrWhiteSpace(search))

            {

                var term = search.Trim();

                query = query.Where(p =>

                    p.ProductName.Contains(term) ||

                    p.ProductCode.Contains(term) ||

                    p.CategoryName.Contains(term));

            }



            var items = await query

                .OrderBy(p => p.ProductName)

                .ToListAsync();



            return Ok(items);

        }

    }

}


