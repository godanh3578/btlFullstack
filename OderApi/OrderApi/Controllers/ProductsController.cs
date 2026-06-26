using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OrderApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class ProductsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ProductsController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet("products")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProducts()
        {
            return await ProxyAsync("/gateway/product/api/products");
        }

        [HttpGet("products/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProduct(int id)
        {
            return await ProxyAsync($"/gateway/product/api/products/{id}");
        }

        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories()
        {
            return await ProxyAsync("/gateway/product/api/categories");
        }

        private async Task<IActionResult> ProxyAsync(string path)
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["ProductIntegration:GatewayBaseUrl"] ?? "http://localhost:7000";
            client.BaseAddress = new Uri(baseUrl);

            try
            {
                using var response = await client.GetAsync(path);
                var content = await response.Content.ReadAsStringAsync();
                return new ContentResult
                {
                    Content = content,
                    ContentType = "application/json",
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "Khong ket noi duoc den kho san pham.",
                    detail = ex.Message
                });
            }
        }
    }
}
