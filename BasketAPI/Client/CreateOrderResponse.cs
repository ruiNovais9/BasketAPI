namespace BasketAPI.Client
{
    public class CreateOrderResponse
    {
        public string OrderId { get; set; }
        public string UserEmail { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderLine> OrderLines { get; set; }
    }
}
