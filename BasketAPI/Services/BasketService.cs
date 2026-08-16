using BasketAPI.Client;
using BasketAPI.Domain;
using BasketAPI.Interfaces;
using System.Collections.Concurrent;

namespace BasketAPI.Services
{
    public class BasketService : IBasketService
    {
        private readonly ConcurrentDictionary<Guid, Basket> _baskets = new();
        private readonly IProductService _productService;

        public BasketService(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<Basket> CreateBasket(string user)
        {
            var basket = new Basket(user);

            _baskets[basket.Id] = basket;
            return basket;
        }

        public async Task<Basket> GetBasket(Guid basketId)
        {
            _baskets.TryGetValue(basketId, out Basket basket);

            return basket;
        }
        public async Task<Basket> UpdateBasket(Guid basketId, string orderId)
        {
            _baskets.TryGetValue(basketId, out Basket basket);
            basket.AlreadyOrder = true;
            basket.OrderId = orderId;
            return basket;
        }
        public async Task<ProductResponse?> GetProductById(int productId) => await _productService.GetProductById(productId);
    }
}
