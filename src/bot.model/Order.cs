namespace bot.model
{
    public class Order
    {
        public string Id { get; set; }
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public string Pair { get; set; }
        public OrderType OrderType { get; set; }
        
    }
}
