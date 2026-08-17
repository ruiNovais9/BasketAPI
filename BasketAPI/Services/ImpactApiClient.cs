using BasketAPI.Client;
using BasketAPI.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BasketAPI.Services
{
    public class ImpactApiClient : IImpactApiClient
    {
        private const string _productsCacheKey = "products";
        private readonly HttpClient _httpClient;
        private IMemoryCache _productInCache;
        internal const string _emailDefault = "teste@outlook.pt";
        public ImpactApiClient(HttpClient httpClient, IMemoryCache productInCache)
        {
            _httpClient = httpClient;
            _productInCache = productInCache;
        }

        public async Task<string> GetToken(string email)
        {
            var request = new LoginRequest
            {
                Email = email
            };

            HttpResponseMessage httpResponse = await _httpClient.PostAsJsonAsync(ApiEndpoints.Login, request);

            httpResponse.EnsureSuccessStatusCode();

            var loginResponse = await httpResponse.Content.ReadFromJsonAsync<LoginResponse>();

            if (loginResponse == null || string.IsNullOrWhiteSpace(loginResponse.Token))
            {
                throw new InvalidOperationException("Unauthorized");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.Token);
            return loginResponse.Token;
        }

        public async Task<CreateOrderResponse> GetOrder(string orderId)
        {
            if (_httpClient.DefaultRequestHeaders.Authorization == null)
            {
                await GetToken(_emailDefault);
            }

            HttpResponseMessage httpResponse = await _httpClient.GetAsync($"{ApiEndpoints.GetOrder}/{orderId}");

            httpResponse.EnsureSuccessStatusCode();

            CreateOrderResponse order = await httpResponse.Content.ReadFromJsonAsync<CreateOrderResponse>() ?? new CreateOrderResponse();

            return order;
        }

        public async Task<List<ProductResponse>> GetProducts()
        {
            if (_httpClient.DefaultRequestHeaders.Authorization == null)
            {
                await GetToken(_emailDefault);
            }

            HttpResponseMessage httpResponse = await _httpClient.GetAsync(ApiEndpoints.GetAllProducts);

            httpResponse.EnsureSuccessStatusCode();

            List<ProductResponse> products = await httpResponse.Content.ReadFromJsonAsync<List<ProductResponse>>() ?? new List<ProductResponse>();

            if (products.Count > 0)
            {
                _productInCache.Set(_productsCacheKey, products, TimeSpan.FromMinutes(30));
            }

            return products;
        }

        public async Task<CreateOrderResponse> CreateOrder(CreateOrderRequest createOrderRequest)
        {
            if (_httpClient.DefaultRequestHeaders.Authorization == null)
            {
                await GetToken(_emailDefault);
            }

            HttpResponseMessage httpResponse = await _httpClient.PostAsJsonAsync(ApiEndpoints.CreateOrder, createOrderRequest);

            httpResponse.EnsureSuccessStatusCode();

            return await httpResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();
        }

        public async Task<IReadOnlyList<ProductResponse>> GetCachedProducts()
        {
            if (_productInCache.TryGetValue(_productsCacheKey, out List<ProductResponse> products))
            {
                return products;
            }

            await GetProducts();

            _productInCache.TryGetValue(_productsCacheKey, out products);
            return products ?? new List<ProductResponse>();
        }
    }
}
