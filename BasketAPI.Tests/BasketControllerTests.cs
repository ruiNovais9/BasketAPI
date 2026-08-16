using BasketAPI.Client;
using BasketAPI.Controllers;
using BasketAPI.Domain;
using BasketAPI.DTOs;
using BasketAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BasketAPI.Tests
{
    [TestClass]
    public sealed class BasketControllerTests
    {
        private readonly Mock<IBasketService> _basketServiceMock = new();
        private readonly Mock<IProductService> _productServiceMock = new();
        private readonly BasketController _basketController;

        public BasketControllerTests()
        {
            _basketController = new BasketController(Mock.Of<Microsoft.Extensions.Logging.ILogger<BasketController>>(), _basketServiceMock.Object, _productServiceMock.Object);
        }

        private static ProductResponse EligibleProduct(int id = 1, decimal price = 10) =>
            new() { Id = id, Name = "Product " + id, Price = price, Size = 1, Stars = 5 };

        private void SetupTopProducts(params ProductResponse[] products) =>
            _productServiceMock.Setup(s => s.GetTopProducts()).ReturnsAsync(products.ToList());

        [TestMethod]
        public async Task Get_ReturnsNotFound_WhenBasketDoesNotExist()
        {
            _basketServiceMock.Setup(s => s.GetBasket(It.IsAny<Guid>())).ReturnsAsync((Basket)null);

            var result = await _basketController.Get(Guid.NewGuid());

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
        }

        [TestMethod]
        public async Task Get_ReturnsBasket_WhenFound()
        {
            var basket = new Basket("teste");
            basket.Items.Add(new BasketItem { ProductId = 1, Quantity = 2, Price = 20, IndividualPrice = 10 });
            basket.TotalPrice = 20;

            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);

            var result = await _basketController.Get(basket.Id);

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            var ok = (OkObjectResult)result.Result;
            var response = (OrderResponse)ok.Value;

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(basket.TotalPrice, response.Order.TotalAmount);
            Assert.AreEqual(basket.User, response.Order.UserEmail);
            Assert.AreEqual(1, response.Order.OrderLines.Count);
            Assert.AreEqual(basket.Items[0].ProductId, response.Order.OrderLines[0].ProductId);
        }

        [TestMethod]
        public async Task Create_ReturnsNewBasketId()
        {
            var basket = new Basket("teste");
            _basketServiceMock.Setup(s => s.CreateBasket(It.IsAny<string>())).ReturnsAsync(basket);

            var result = await _basketController.Create();

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            var ok = (OkObjectResult)result.Result;
            Assert.AreEqual(basket.Id, ok.Value);
        }

        [TestMethod]
        public async Task Add_ReturnsBadRequest_WhenValidationFails()
        {
            var request = new AddProductToBasketRequest { ProductId = 0, Quantity = 0, BasketId = Guid.NewGuid() };

            var result = await _basketController.Add(request);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Add_ReturnsNotFound_WhenBasketDoesNotExist()
        {
            var request = new AddProductToBasketRequest { ProductId = 1, Quantity = 1, BasketId = Guid.NewGuid() };
            _basketServiceMock.Setup(s => s.GetBasket(request.BasketId)).ReturnsAsync(new Basket(""));

            var result = await _basketController.Add(request);

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
        }

        [TestMethod]
        public async Task Add_ReturnsBadRequest_WhenBasketAlreadyOrdered()
        {
            var basket = new Basket("teste") { AlreadyOrder = true };
            var request = new AddProductToBasketRequest { ProductId = 1, Quantity = 1, BasketId = basket.Id };
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);

            var result = await _basketController.Add(request);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Add_ReturnsNotFound_WhenProductDoesNotExist()
        {
            var basket = new Basket("teste");
            var request = new AddProductToBasketRequest { ProductId = 1, Quantity = 1, BasketId = basket.Id };
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);
            _basketServiceMock.Setup(s => s.GetProductById(1)).ReturnsAsync((ProductResponse)null);

            var result = await _basketController.Add(request);

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
        }

        [TestMethod]
        public async Task Add_ReturnsBadRequest_WhenProductNotInTop100()
        {
            var basket = new Basket("teste");
            var product = EligibleProduct();
            var request = new AddProductToBasketRequest { ProductId = 1, Quantity = 1, BasketId = basket.Id };
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);
            _basketServiceMock.Setup(s => s.GetProductById(1)).ReturnsAsync(product);
            SetupTopProducts();

            var result = await _basketController.Add(request);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Add_AddsNewItem_WhenProductNotYetInBasket()
        {
            var basket = new Basket("teste");
            var product = EligibleProduct(price: 10);
            var request = new AddProductToBasketRequest { ProductId = 1, Quantity = 2, BasketId = basket.Id };
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);
            _basketServiceMock.Setup(s => s.GetProductById(1)).ReturnsAsync(product);
            SetupTopProducts(product);

            var result = await _basketController.Add(request);

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            Assert.AreEqual(1, basket.Items.Count);
            Assert.AreEqual(2, basket.Items[0].Quantity);
            Assert.AreEqual(20, basket.TotalPrice);
        }

        [TestMethod]
        public async Task Add_IncreasesQuantity_WhenProductAlreadyInBasket()
        {
            var basket = new Basket("teste");
            var product = EligibleProduct(price: 10);
            basket.Items.Add(new BasketItem { ProductId = 1, Quantity = 2, Price = 20, IndividualPrice = 10 });
            basket.TotalPrice = 20;

            var request = new AddProductToBasketRequest { ProductId = 1, Quantity = 3, BasketId = basket.Id };
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);
            _basketServiceMock.Setup(s => s.GetProductById(1)).ReturnsAsync(product);
            SetupTopProducts(product);

            var result = await _basketController.Add(request);

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            Assert.AreEqual(5, basket.Items[0].Quantity);
            Assert.AreEqual(50, basket.Items[0].Price);
            Assert.AreEqual(50, basket.TotalPrice);
        }

        [TestMethod]
        public async Task Delete_ReturnsBadRequest_WhenProductIdInvalid()
        {
            var result = await _basketController.Delete(Guid.NewGuid(), 0);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Delete_ReturnsNotFound_WhenBasketDoesNotExist()
        {
            _basketServiceMock.Setup(s => s.GetBasket(It.IsAny<Guid>())).ReturnsAsync((Basket)null!);

            var result = await _basketController.Delete(Guid.NewGuid(), 1);

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
        }

        [TestMethod]
        public async Task Delete_ReturnsBadRequest_WhenBasketAlreadyOrdered()
        {
            var basket = new Basket("teste") { AlreadyOrder = true };
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);

            var result = await _basketController.Delete(basket.Id, 1);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Delete_ReturnsNotFound_WhenProductNotInBasket()
        {
            var basket = new Basket("teste");
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);

            var result = await _basketController.Delete(basket.Id, 1);

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
        }

        [TestMethod]
        public async Task Delete_RemovesItem_AndRecalculatesTotal()
        {
            var basket = new Basket("teste");
            basket.Items.Add(new BasketItem { ProductId = 1, Quantity = 2, Price = 20, IndividualPrice = 10 });
            basket.Items.Add(new BasketItem { ProductId = 2, Quantity = 1, Price = 5, IndividualPrice = 5 });
            basket.TotalPrice = 25;
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);

            var result = await _basketController.Delete(basket.Id, 1);

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            Assert.AreEqual(1, basket.Items.Count);
            Assert.AreEqual(5, basket.TotalPrice);
        }

        [TestMethod]
        public async Task Update_ReturnsBadRequest_WhenValidationFails()
        {
            var request = new UpdateProductToBasketRequest { ProductId = 0, BasketId = Guid.NewGuid() };

            var result = await _basketController.Update(request);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Update_ReturnsNotFound_WhenBasketDoesNotExist()
        {
            var request = new UpdateProductToBasketRequest { ProductId = 1, BasketId = Guid.NewGuid() };
            _basketServiceMock.Setup(s => s.GetBasket(request.BasketId)).ReturnsAsync((Basket)null!);

            var result = await _basketController.Update(request);

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
        }

        [TestMethod]
        public async Task Update_ReturnsBadRequest_WhenBasketAlreadyOrdered()
        {
            var basket = new Basket("teste") { AlreadyOrder = true };
            var request = new UpdateProductToBasketRequest { ProductId = 1, BasketId = basket.Id };
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);

            var result = await _basketController.Update(request);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Update_ReturnsNotFound_WhenProductDoesNotExist()
        {
            var basket = new Basket("teste");
            var request = new UpdateProductToBasketRequest { ProductId = 1, BasketId = basket.Id };
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);
            _basketServiceMock.Setup(s => s.GetProductById(1)).ReturnsAsync((ProductResponse)null);

            var result = await _basketController.Update(request);

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
        }

        [TestMethod]
        public async Task Update_ReturnsBadRequest_WhenProductNotInTop100()
        {
            var basket = new Basket("teste");
            var product = EligibleProduct();
            var request = new UpdateProductToBasketRequest { ProductId = 1, Quantity = 1, BasketId = basket.Id };
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);
            _basketServiceMock.Setup(s => s.GetProductById(1)).ReturnsAsync(product);
            SetupTopProducts();

            var result = await _basketController.Update(request);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Update_ReturnsBadRequest_WhenNewItemWithZeroQuantity()
        {
            var basket = new Basket("teste");
            var product = EligibleProduct();
            var request = new UpdateProductToBasketRequest { ProductId = 1, Quantity = 0, BasketId = basket.Id };
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);
            _basketServiceMock.Setup(s => s.GetProductById(1)).ReturnsAsync(product);
            SetupTopProducts(product);

            var result = await _basketController.Update(request);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Update_AddsNewItem_WhenQuantityPositive()
        {
            var basket = new Basket("teste");
            var product = EligibleProduct(price: 10);
            var request = new UpdateProductToBasketRequest { ProductId = 1, Quantity = 4, BasketId = basket.Id };
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);
            _basketServiceMock.Setup(s => s.GetProductById(1)).ReturnsAsync(product);
            SetupTopProducts(product);

            var result = await _basketController.Update(request);

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            Assert.AreEqual(1, basket.Items.Count);
            Assert.AreEqual(40, basket.TotalPrice);
        }

        [TestMethod]
        public async Task Update_RemovesItem_WhenQuantityIsZeroOrLess()
        {
            var basket = new Basket("teste");
            var product = EligibleProduct(price: 10);
            basket.Items.Add(new BasketItem { ProductId = 1, Quantity = 2, Price = 20, IndividualPrice = 10 });
            basket.TotalPrice = 20;

            var request = new UpdateProductToBasketRequest { ProductId = 1, Quantity = 0, BasketId = basket.Id };
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);
            _basketServiceMock.Setup(s => s.GetProductById(1)).ReturnsAsync(product);
            SetupTopProducts(product);

            var result = await _basketController.Update(request);

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            Assert.AreEqual(0, basket.Items.Count);
            Assert.AreEqual(0, basket.TotalPrice);
        }

        [TestMethod]
        public async Task Update_ChangesQuantity_WhenItemExistsAndQuantityPositive()
        {
            var basket = new Basket("teste");
            var product = EligibleProduct(price: 10);
            basket.Items.Add(new BasketItem { ProductId = 1, Quantity = 2, Price = 20, IndividualPrice = 10 });
            basket.TotalPrice = 20;

            var request = new UpdateProductToBasketRequest { ProductId = 1, Quantity = 5, BasketId = basket.Id };
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);
            _basketServiceMock.Setup(s => s.GetProductById(1)).ReturnsAsync(product);
            SetupTopProducts(product);

            var result = await _basketController.Update(request);

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            Assert.AreEqual(5, basket.Items[0].Quantity);
            Assert.AreEqual(50, basket.TotalPrice);
        }

        [TestMethod]
        public async Task CreatOrder_ReturnsNotFound_WhenBasketDoesNotExist()
        {
            _basketServiceMock.Setup(s => s.GetBasket(It.IsAny<Guid>())).ReturnsAsync((Basket)null!);

            var result = await _basketController.CreatOrder(Guid.NewGuid());

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
        }

        [TestMethod]
        public async Task CreatOrder_ReturnsBadRequest_WhenBasketAlreadyOrdered()
        {
            var basket = new Basket("teste") { AlreadyOrder = true };
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);

            var result = await _basketController.CreatOrder(basket.Id);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task CreatOrder_ReturnsBadRequest_WhenBasketIsEmpty()
        {
            var basket = new Basket("teste");
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);

            var result = await _basketController.CreatOrder(basket.Id);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task CreatOrder_ReturnsNotFound_WhenSomeProductsNoLongerExist()
        {
            var basket = new Basket("teste");
            basket.Items.Add(new BasketItem { ProductId = 1, Quantity = 1, Price = 10, IndividualPrice = 10 });
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);
            _productServiceMock.Setup(s => s.GetProductByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductResponse>());

            var result = await _basketController.CreatOrder(basket.Id);

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
        }

        [TestMethod]
        public async Task CreatOrder_ReturnsBadRequest_WhenOrderResponseIsNull()
        {
            var basket = new Basket("teste");
            basket.Items.Add(new BasketItem { ProductId = 1, Quantity = 1, Price = 10, IndividualPrice = 10 });
            var product = EligibleProduct();
            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);
            _productServiceMock.Setup(s => s.GetProductByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductResponse> { product });
            _productServiceMock.Setup(s => s.CreateOrder(It.IsAny<CreateOrderRequest>())).ReturnsAsync((CreateOrderResponse)null!);

            var result = await _basketController.CreatOrder(basket.Id);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task CreatOrder_ReturnsOk_AndMarksBasketAsOrdered_OnSuccess()
        {
            var basket = new Basket("teste");
            basket.Items.Add(new BasketItem { ProductId = 1, Quantity = 1, Price = 10, IndividualPrice = 10 });
            var product = EligibleProduct();
            var order = new CreateOrderResponse { OrderId = "order-1" };

            _basketServiceMock.Setup(s => s.GetBasket(basket.Id)).ReturnsAsync(basket);
            _productServiceMock.Setup(s => s.GetProductByIds(It.IsAny<List<int>>())).ReturnsAsync(new List<ProductResponse> { product });
            _productServiceMock.Setup(s => s.CreateOrder(It.IsAny<CreateOrderRequest>())).ReturnsAsync(order);

            var result = await _basketController.CreatOrder(basket.Id);

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            _basketServiceMock.Verify(s => s.UpdateBasket(basket.Id, "order-1"), Times.Once);
        }

        [TestMethod]
        public async Task GetOrder_ReturnsOkWithOrder()
        {
            var order = new CreateOrderResponse { OrderId = "abc" };
            _productServiceMock.Setup(s => s.GetOrder(It.IsAny<string>())).ReturnsAsync(order);

            var result = await _basketController.GetOrder(Guid.NewGuid().ToString());

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            var ok = (OkObjectResult)result.Result;
            var response = (OrderResponse)ok.Value;
            Assert.AreEqual("abc", response.Order.OrderId);
        }
    }
}