namespace BasketAPI.Domain
{
    public class Basket
    {
        public Basket(string user)
        {
            Id = Guid.NewGuid();
            Items = new List<BasketItem>();
            CreateDate = DateTime.UtcNow;
            TotalPrice = 0;
            User = user;
        }
        public Guid Id { get; set; }
        public List<BasketItem> Items { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string User { get; set; }
        public decimal TotalPrice { get; set; }
        public bool AlreadyOrder { get; set; }
        public string OrderId { get; set; }
    }
}
