using BasketAPI.Client;
using BasketAPI.Domain;

namespace BasketAPI.Interfaces
{
    public interface IBasketService
    {
        Task<Basket> CreateBasket(string user);
        Task<Basket> GetBasket(Guid basketId);
        Task<ProductResponse?> GetProductById(int productId);
        Task<Basket> UpdateBasket(Guid basketId, string orderId);
    }
}
