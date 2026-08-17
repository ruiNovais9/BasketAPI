using BasketAPI.Client;
using BasketAPI.Controllers;
using BasketAPI.Interfaces;
using Moq;

namespace BasketAPI.Tests
{
    [TestClass]
    public sealed class ProductsControllerTests
    {
        private readonly Mock<IProductService> _productServiceMock = new();
        private readonly ProductsController _productsController;

        public ProductsControllerTests()
        {
            _productsController = new ProductsController(Mock.Of<Microsoft.Extensions.Logging.ILogger<ProductsController>>(), _productServiceMock.Object);
        }

        [TestMethod]
        public async Task GetTopProducts_ReturnsServiceResult()
        {
            var products = new List<ProductResponse> { new() { Id = 1 } };
            _productServiceMock.Setup(s => s.GetTopProducts()).ReturnsAsync(products);

            var result = await _productsController.GetTopProducts();

            CollectionAssert.AreEqual(products, (List<ProductResponse>)result.Value);
        }

        [TestMethod]
        public async Task GetTenCheapestProduct_ReturnsServiceResult()
        {
            var products = new List<ProductResponse> { new() { Id = 2 } };
            _productServiceMock.Setup(s => s.GetTenCheapestProduct()).ReturnsAsync(products);

            var result = await _productsController.GetTenCheapestProduct();

            CollectionAssert.AreEqual(products, (List<ProductResponse>)result.Value);
        }

        [TestMethod]
        public async Task GetProductsByPage_ReturnsServiceResult()
        {
            var products = new List<ProductResponse> { new() { Id = 3 } };
            _productServiceMock.Setup(s => s.GetProductsByPage(0, 10)).ReturnsAsync(products);

            var result = await _productsController.GetProductsByPage(0, 10);

            CollectionAssert.AreEqual(products, (List<ProductResponse>)result.Value);
        }
    }
}