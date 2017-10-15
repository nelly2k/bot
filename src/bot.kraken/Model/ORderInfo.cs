using System;

namespace bot.kraken.Model
{
    public class OrderInfo
    {
        public string Id { get; set; }
        public KrakenOrderStatus Status { get; set; }

        public string Reason { get; set; }

        public DateTime OpenDateTime { get; set; }
        public DateTime ClosedDateTime { get; set; }

        public string Pair { get; set; }
        public OrderType OrderType { get; set; }
        public OrderPriceType OrderPriceType { get; set; }

        public decimal Price { get; set; }
        public decimal PrimaryPrice { get; set; }
        public decimal SecondaryPrice { get; set; }

        public string Leverage { get; set; }
        public decimal Volume { get; set; }
        public decimal VolumeExec { get; set; }
        public decimal Cost { get; set; }
        public decimal Fee { get; set; }
        public string Misc { get; set; }
    }
}