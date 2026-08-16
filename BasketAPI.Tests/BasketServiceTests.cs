using BasketAPI.Client;
using BasketAPI.Interfaces;
using BasketAPI.Services;
using Moq;

namespace BasketAPI.Tests
{
    [TestClass]
    public sealed class BasketServiceTests
    {
        private readonly Mock<IProductService> _productServiceMock = new();
        private readonly BasketService _basketService;

        public BasketServiceTests()
        {
            _basketService = new BasketService(_productServiceMock.Object);
        }

        [TestMethod]
        public async Task CreateBasket_GeneratesUniqueGuid()
        {
            var basket1 = await _basketService.CreateBasket("teste");
            var basket2 = await _basketService.CreateBasket("teste");

            Assert.AreNotEqual(basket1.Id, basket2.Id);
        }

        [TestMethod]
        public async Task GetBasket_ReturnsNull_WhenNotFound()
        {
            var result = await _basketService.GetBasket(Guid.NewGuid());

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetBasket_ReturnsBasket_AfterCreate()
        {
            var created = await _basketService.CreateBasket("teste");

            var result = await _basketService.GetBasket(created.Id);

            Assert.AreEqual(created.Id, result!.Id);
        }

        [TestMethod]
        public async Task UpdateBasket_MarksBasketAsOrdered_AndSetsOrderId()
        {
            var created = await _basketService.CreateBasket("teste");

            var result = await _basketService.UpdateBasket(created.Id, "order-99");

            Assert.IsTrue(result.AlreadyOrder);
            Assert.AreEqual("order-99", result.OrderId);
        }

        [TestMethod]
        public async Task GetProductById_DelegatesToProductService()
        {
            var product = new ProductResponse { Id = 7, Name = "X" };
            _productServiceMock.Setup(s => s.GetProductById(7)).ReturnsAsync(product);

            var result = await _basketService.GetProductById(7);

            Assert.AreEqual("X", result!.Name);
        }
    }
}
