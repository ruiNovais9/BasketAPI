using BasketAPI.Client;
using BasketAPI.Domain;

namespace BasketAPI.DTOs
{
    public class OrderResponse : GenericResponse
    {
        public OrderResponse(CreateOrderResponse order)
        {
            IsSuccess = true;
            Order = order;
        }

        public OrderResponse(Basket basket)
        {
            IsSuccess = true;
            Order = new CreateOrderResponse
            {
                UserEmail = basket.User,
                OrderId = basket.OrderId,
                TotalAmount = basket.TotalPrice,
                OrderLines = basket.Items.Select(x => new OrderLine
                {
                    TotalPrice = x.Price,
                    ProductId = x.ProductId,
                    ProductUnitPrice = x.IndividualPrice,
                    Quantity = x.Quantity
                }).ToList()
            };
        }

        public OrderResponse(string errorMessage) : base (errorMessage)
        {
            IsSuccess = false;
            Order = null;
        }
        public CreateOrderResponse Order { get; set; } = new CreateOrderResponse();
    }
}
