using System.Net;
using System.Text;
using BasketAPI.Client;
using BasketAPI.Services;
using Microsoft.Extensions.Caching.Memory;

namespace BasketAPI.Tests
{
    public sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public List<string> RequestUris { get; } = new();

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!.ToString());
            return Task.FromResult(_responder(request));
        }
    }

    [TestClass]
    public sealed class ImpactApiClientTests
    {
        private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) =>
            new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        private static (ImpactApiClient client, FakeHttpMessageHandler handler, IMemoryCache cache) CreateSut(
            Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            var handler = new FakeHttpMessageHandler(responder);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fake.test/api/") };
            var cache = new MemoryCache(new MemoryCacheOptions());
            var client = new ImpactApiClient(httpClient, cache);
            return (client, handler, cache);
        }

        private static HttpResponseMessage RouteLoginOrDefault(HttpRequestMessage req, string defaultJson) =>
            req.RequestUri!.AbsolutePath.Contains("Login")
                ? JsonResponse(HttpStatusCode.OK, "{\"token\":\"abc\"}")
                : JsonResponse(HttpStatusCode.OK, defaultJson);

        [TestMethod]
        public async Task GetToken_SetsAuthorizationHeader_WhenTokenIsValid()
        {
            var (client, _, _) = CreateSut(_ => JsonResponse(HttpStatusCode.OK, "{\"token\":\"abc123\"}"));

            var token = await client.GetToken("teste@teste.com");

            Assert.AreEqual("abc123", token);
        }

        [TestMethod]
        public async Task GetToken_Throws_WhenTokenIsEmpty()
        {
            var (client, _, _) = CreateSut(_ => JsonResponse(HttpStatusCode.OK, "{\"token\":\"\"}"));

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => client.GetToken("teste@teste.com"));
        }

        [TestMethod]
        public async Task GetToken_Throws_WhenResponseBodyIsNull()
        {
            var (client, _, _) = CreateSut(_ => JsonResponse(HttpStatusCode.OK, "null"));

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => client.GetToken("teste@teste.com"));
        }

        [TestMethod]
        public async Task GetOrder_CallsCorrectUrl_WithOrderIdAppended()
        {
            var (client, handler, _) = CreateSut(req => RouteLoginOrDefault(req,
                "{\"orderId\":\"abc123\",\"userEmail\":\"teste\",\"totalAmount\":10,\"orderLines\":[]}"));

            var order = await client.GetOrder("abc123");

            Assert.AreEqual("abc123", order.OrderId);
            Assert.IsTrue(handler.RequestUris.Last().EndsWith("GetOrder/abc123"));
        }

        [TestMethod]
        public async Task GetProducts_TriggersLogin_WhenNotYetAuthenticated()
        {
            var (client, handler, _) = CreateSut(req => RouteLoginOrDefault(req, "[]"));

            await client.GetProducts();

            Assert.IsTrue(handler.RequestUris.Any(u => u.Contains("Login")));
        }

        [TestMethod]
        public async Task GetProducts_DoesNotCallLoginAgain_WhenAlreadyAuthenticated()
        {
            var (client, handler, _) = CreateSut(req => RouteLoginOrDefault(req, "[]"));

            await client.GetToken("teste@teste.com");
            await client.GetProducts();

            Assert.AreEqual(1, handler.RequestUris.Count(u => u.Contains("Login")));
        }

        [TestMethod]
        public async Task GetProducts_Throws_WhenApiReturnsError()
        {
            var (client, _, _) = CreateSut(req =>
                req.RequestUri!.AbsolutePath.Contains("Login")
                    ? JsonResponse(HttpStatusCode.OK, "{\"token\":\"abc\"}")
                    : new HttpResponseMessage(HttpStatusCode.InternalServerError));

            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => client.GetProducts());
        }

        [TestMethod]
        public async Task CreateOrder_ReturnsDeserializedResponse()
        {
            var (client, _, _) = CreateSut(req => RouteLoginOrDefault(req,
                "{\"orderId\":\"o1\",\"userEmail\":\"teste\",\"totalAmount\":5,\"orderLines\":[]}"));

            var result = await client.CreateOrder(new CreateOrderRequest());

            Assert.AreEqual("o1", result.OrderId);
        }

        [TestMethod]
        public async Task GetCachedProducts_ReturnsCached_WhenPresent_WithoutHttpCall()
        {
            var (client, handler, cache) = CreateSut(_ => JsonResponse(HttpStatusCode.OK, "[]"));
            cache.Set("products", new List<ProductResponse> { new() { Id = 1 } }, TimeSpan.FromMinutes(5));

            var result = await client.GetCachedProducts();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(0, handler.RequestUris.Count);
        }

        [TestMethod]
        public async Task GetCachedProducts_FetchesFromApi_WhenCacheEmpty()
        {
            var (client, handler, _) = CreateSut(req => RouteLoginOrDefault(req,
                "[{\"id\":1,\"name\":\"P\",\"price\":1,\"size\":1,\"stars\":1}]"));

            var result = await client.GetCachedProducts();

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(handler.RequestUris.Any(u => u.Contains("GetAllProducts")));
        }
    }
}