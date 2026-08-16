using BasketAPI.Client;
using BasketAPI.Interfaces;
using BasketAPI.Services;
using Moq;

namespace BasketAPI.Tests
{
    [TestClass]
    public sealed class ProductServiceTests
    {
        private readonly Mock<IImpactApiClient> _impactApiClientMock = new();
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _productService = new ProductService(_impactApiClientMock.Object);
        }

        [TestMethod]
        public async Task GetToken_DelegatesToClient()
        {
            _impactApiClientMock.Setup(c => c.GetToken(It.IsAny<string>())).ReturnsAsync("token-123");

            var result = await _productService.GetToken();

            Assert.AreEqual("token-123", result);
        }

        [TestMethod]
        public async Task GetProducts_DelegatesToClient()
        {
            var products = new List<ProductResponse> { new() { Id = 1 } };
            _impactApiClientMock.Setup(c => c.GetProducts()).ReturnsAsync(products);

            var result = await _productService.GetProducts();

            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public async Task GetOrder_DelegatesToClient()
        {
            var order = new CreateOrderResponse { OrderId = "abc" };
            _impactApiClientMock.Setup(c => c.GetOrder("abc")).ReturnsAsync(order);

            var result = await _productService.GetOrder("abc");

            Assert.AreEqual("abc", result.OrderId);
        }

        [TestMethod]
        public async Task GetProductById_ReturnsProduct_WhenFound()
        {
            var products = new List<ProductResponse> { new() { Id = 5, Name = "X" } };
            _impactApiClientMock.Setup(c => c.GetCachedProducts()).ReturnsAsync(products);

            var result = await _productService.GetProductById(5);

            Assert.IsNotNull(result);
            Assert.AreEqual("X", result!.Name);
        }

        [TestMethod]
        public async Task GetProductById_ReturnsNull_WhenNotFound()
        {
            _impactApiClientMock.Setup(c => c.GetCachedProducts()).ReturnsAsync(new List<ProductResponse>());

            var result = await _productService.GetProductById(999);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetProductByIds_ReturnsOnlyMatchingProducts()
        {
            var products = new List<ProductResponse> { new() { Id = 1 }, new() { Id = 2 }, new() { Id = 3 } };
            _impactApiClientMock.Setup(c => c.GetCachedProducts()).ReturnsAsync(products);

            var result = await _productService.GetProductByIds(new List<int> { 1, 3 });

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public async Task GetTopProducts_ReturnsTop100OrderedByStarsDescending()
        {
            var products = Enumerable.Range(1, 150)
                .Select(i => new ProductResponse { Id = i, Name = $"P{i}", Price = 10, Size = 1, Stars = i % 5 })
                .ToList();

            _impactApiClientMock.Setup(c => c.GetCachedProducts()).ReturnsAsync(products);

            var result = await _productService.GetTopProducts();

            Assert.AreEqual(100, result.Count);
            CollectionAssert.AreEqual(
                result.OrderByDescending(p => p.Stars).Select(p => p.Id).ToList(),
                result.Select(p => p.Id).ToList());
        }

        [TestMethod]
        public async Task GetProductsByPage_ClampsNegativePageToZero()
        {
            var products = Enumerable.Range(1, 10).Select(i => new ProductResponse { Id = i, Price = i }).ToList();
            _impactApiClientMock.Setup(c => c.GetCachedProducts()).ReturnsAsync(products);

            var result = await _productService.GetProductsByPage(-5, 10);

            Assert.AreEqual(10, result.Count);
        }

        [TestMethod]
        public async Task GetProductsByPage_ClampsTakeTo1000()
        {
            var products = Enumerable.Range(1, 2000).Select(i => new ProductResponse { Id = i, Price = i }).ToList();
            _impactApiClientMock.Setup(c => c.GetCachedProducts()).ReturnsAsync(products);

            var result = await _productService.GetProductsByPage(0, 5000);

            Assert.AreEqual(1000, result.Count);
        }

        [TestMethod]
        public async Task GetProductsByPage_OrdersByPriceAscending()
        {
            var products = new List<ProductResponse>
            {
                new() { Id = 1, Price = 30 },
                new() { Id = 2, Price = 10 },
                new() { Id = 3, Price = 20 },
            };
            _impactApiClientMock.Setup(c => c.GetCachedProducts()).ReturnsAsync(products);

            var result = await _productService.GetProductsByPage(0, 10);

            CollectionAssert.AreEqual(new[] { 2, 3, 1 }, result.Select(p => p.Id).ToArray());
        }

        [TestMethod]
        public async Task GetTenCheapestProduct_ReturnsTenCheapest()
        {
            var products = Enumerable.Range(1, 50).Select(i => new ProductResponse { Id = i, Price = 50 - i }).ToList();
            _impactApiClientMock.Setup(c => c.GetCachedProducts()).ReturnsAsync(products);

            var result = await _productService.GetTenCheapestProduct();

            Assert.AreEqual(10, result.Count);
            Assert.IsTrue(result.All(p => p.Price <= 40));
        }

        [TestMethod]
        public async Task CreateOrder_DelegatesToClient()
        {
            var request = new CreateOrderRequest();
            var response = new CreateOrderResponse { OrderId = "xyz" };
            _impactApiClientMock.Setup(c => c.CreateOrder(request)).ReturnsAsync(response);

            var result = await _productService.CreateOrder(request);

            Assert.AreEqual("xyz", result.OrderId);
        }
    }
}