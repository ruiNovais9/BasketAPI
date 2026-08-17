using BasketAPI.Client;
using BasketAPI.Interfaces;

namespace BasketAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly IImpactApiClient _impactApiClient;
        private const int _maxPageSize = 1000;
        public ProductService(IImpactApiClient impactApiClient)
        {
            _impactApiClient = impactApiClient;
        }

        public async Task<string> GetToken()
        {
            return await _impactApiClient.GetToken(ImpactApiClient._emailDefault);
        }

        public async Task<CreateOrderResponse> GetOrder(string orderId)
        {
            return await _impactApiClient.GetOrder(orderId);
        }

        public async Task<ProductResponse> GetProductById(int productId)
        {
            var products = await _impactApiClient.GetCachedProducts();

            return products.FirstOrDefault(x => x.Id == productId);
        }

        public async Task<List<ProductResponse>> GetProductByIds(List<int> productId)
        {
            var products = await _impactApiClient.GetCachedProducts();

            return products.Where(x => productId.Contains(x.Id)).ToList();
        }

        public async Task<List<ProductResponse>> GetTopProducts()
        {
            var products = await _impactApiClient.GetCachedProducts();

            return products.OrderByDescending(x => x.Stars)
                           .Take(100)
                           .ToList();
        }

        public async Task<List<ProductResponse>> GetProductsByPage(int page, int numberOfProducts)
        {
            page = page < 0 ? 0 : page;

            int takeProductsNumber = numberOfProducts > _maxPageSize ? _maxPageSize : numberOfProducts;

            var products = await _impactApiClient.GetCachedProducts();

            return products.OrderBy(x => x.Price)
                           .ThenBy(x => x.Id)
                           .Skip(page * takeProductsNumber)
                           .Take(takeProductsNumber)
                           .ToList();
        }

        public async Task<List<ProductResponse>> GetTenCheapestProduct()
        {
            var products = await _impactApiClient.GetCachedProducts();

            return products.OrderBy(x => x.Price)
                           .Take(10)
                           .ToList();
        }

        public async Task<CreateOrderResponse> CreateOrder(CreateOrderRequest createOrderRequest)
        {
            return await _impactApiClient.CreateOrder(createOrderRequest);
        }
    }
}
