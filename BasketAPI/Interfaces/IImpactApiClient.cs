using BasketAPI.Client;

namespace BasketAPI.Interfaces
{
    public interface IImpactApiClient
    {
        Task<string> GetToken(string email);
        Task<CreateOrderResponse> GetOrder(string orderId);
        Task<List<ProductResponse>> GetProducts();
        Task<CreateOrderResponse> CreateOrder(CreateOrderRequest createOrderRequest);
        Task<IReadOnlyList<ProductResponse>> GetCachedProducts();
    }
}
