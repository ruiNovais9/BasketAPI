using BasketAPI.Client;

namespace BasketAPI.Interfaces
{
    public interface IProductService
    {
        Task<string> GetToken();
        Task<CreateOrderResponse> GetOrder(string orderId);
        Task<ProductResponse> GetProductById(int productId);
        Task<List<ProductResponse>> GetProductByIds(List<int> productId);
        Task<List<ProductResponse>> GetTopProducts();
        Task<List<ProductResponse>> GetProductsByPage(int page, int numberOfProducts);
        Task<List<ProductResponse>> GetTenCheapestProduct();
        Task<CreateOrderResponse> CreateOrder(CreateOrderRequest createOrderRequest);
    }
}
