namespace BasketAPI.Client
{
    public class CreateOrderRequest
    {
        public string UserEmail { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderLine> OrderLines { get; set; }
    }
}
