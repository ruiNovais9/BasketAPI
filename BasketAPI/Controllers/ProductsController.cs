using BasketAPI.Client;
using BasketAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BasketAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ILogger<ProductsController> _logger;

        private readonly IProductService _productService;

        public ProductsController(ILogger<ProductsController> logger, IProductService productService)
        {
            _logger = logger;
            _productService = productService;
        }

        [HttpGet("GetTopProducts")]
        public async Task<ActionResult<List<ProductResponse>>> GetTopProducts()
        {
            return await _productService.GetTopProducts();
        }

        [HttpGet("GetTenCheapestProduct")]
        public async Task<ActionResult<List<ProductResponse>>> GetTenCheapestProduct()
        {
            return await _productService.GetTenCheapestProduct();
        }

        [HttpGet("GetProductsByPage")]
        public async Task<ActionResult<List<ProductResponse>>> GetProductsByPage(int page, int numberOfProduct)
        {
            return await _productService.GetProductsByPage(page, numberOfProduct);
        }

        [HttpGet("GetProducts")]
        public async Task<List<ProductResponse>> GetProducts()
        {
            return await _productService.GetProducts();
        }
    }
}
