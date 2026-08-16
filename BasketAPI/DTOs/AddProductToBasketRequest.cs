namespace BasketAPI.DTOs
{
    public class AddProductToBasketRequest
    {
        public Guid BasketId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
