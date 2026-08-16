using BasketAPI.Client;
using BasketAPI.DTOs;
using BasketAPI.Interfaces;
using BasketAPI.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BasketAPI.Tests
{
    public sealed class BasketApiFactory : WebApplicationFactory<Program>
    {
        public Func<HttpRequestMessage, HttpResponseMessage> CatalogApiResponder { get; set; } = _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            };

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddHttpClient<IImpactApiClient, ImpactApiClient>(client =>
                {
                    client.BaseAddress = new Uri("https://fake.test/api/");
                })
                .ConfigurePrimaryHttpMessageHandler(() => new FakeHttpMessageHandler(req => CatalogApiResponder(req)));
            });
        }
    }

    public sealed class OrderResponseDto
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public CreateOrderResponse Order { get; set; } = new();
    }

    [TestClass]
    public sealed class BasketEndToEndTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private static string BuildProductsJson(int count) =>
            "[" + string.Join(",", Enumerable.Range(1, count)
                .Select(id => $"{{\"id\":{id},\"name\":\"Product {id}\",\"price\":{10 + id},\"size\":1,\"stars\":{count - id}}}")) + "]";

        private static HttpResponseMessage CatalogResponder(HttpRequestMessage req, string productsJson)
        {
            var path = req.RequestUri!.AbsolutePath;

            if (path.Contains("Login"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent("{\"token\":\"fake-token\"}", Encoding.UTF8, "application/json") };
            }
              

            if (path.Contains("GetAllProducts"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(productsJson, Encoding.UTF8, "application/json") };
            }

            if (path.Contains("CreateOrder"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                       "{\"orderId\":\"order-e2e-1\",\"userEmail\":\"teste\",\"totalAmount\":22,\"orderLines\":[]}",
                       Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Create_ThenGet_ReturnsTheSameBasket()
        {
            using var factory = new BasketApiFactory();
            var client = factory.CreateClient();

            var createResponse = await client.PostAsync("/Basket/Create", null);
            createResponse.EnsureSuccessStatusCode();
            var basketId = await createResponse.Content.ReadFromJsonAsync<Guid>();

            var getResponse = await client.GetAsync($"/Basket/Get?guid={basketId}");

            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
            var body = await getResponse.Content.ReadFromJsonAsync<OrderResponseDto>(JsonOptions);
            Assert.IsNotNull(body);
            Assert.AreEqual(0, body.Order.TotalAmount);
        }

        [TestMethod]
        public async Task Add_ReturnsBadRequest_WhenProductIsNotInTop100()
        {
            using var factory = new BasketApiFactory { CatalogApiResponder = req => CatalogResponder(req, BuildProductsJson(150)) };
            var client = factory.CreateClient();

            var createResponse = await client.PostAsync("/Basket/Create", null);
            var basketId = await createResponse.Content.ReadFromJsonAsync<Guid>();

            var addRequest = new AddProductToBasketRequest { BasketId = basketId, ProductId = 150, Quantity = 1 };
            var addResponse = await client.PutAsJsonAsync("/Basket/Add", addRequest);

            Assert.AreEqual(HttpStatusCode.BadRequest, addResponse.StatusCode);
        }

        [TestMethod]
        public async Task FullFlow_CreateAddCheckout_ReturnsOrderConfirmation()
        {
            using var factory = new BasketApiFactory { CatalogApiResponder = req => CatalogResponder(req, BuildProductsJson(150)) };
            var client = factory.CreateClient();

            var createResponse = await client.PostAsync("/Basket/Create", null);
            var basketId = await createResponse.Content.ReadFromJsonAsync<Guid>();

            var addRequest = new AddProductToBasketRequest { BasketId = basketId, ProductId = 1, Quantity = 2 };
            var addResponse = await client.PutAsJsonAsync("/Basket/Add", addRequest);
            Assert.AreEqual(HttpStatusCode.OK, addResponse.StatusCode);

            var checkoutResponse = await client.PostAsync($"/Basket/CreateOrder?guid={basketId}", null);

            Assert.AreEqual(HttpStatusCode.OK, checkoutResponse.StatusCode);
            var order = await checkoutResponse.Content.ReadFromJsonAsync<OrderResponseDto>(JsonOptions);
            Assert.IsNotNull(order);
            Assert.AreEqual("order-e2e-1", order.Order.OrderId);
        }
    }
}