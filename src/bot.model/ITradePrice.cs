using System;

namespace bot.model
{
    public interface ITradePrice
    {
        DateTime DateTime { get; set; }

        decimal Price { get; set; }
        decimal Volume { get; set; }
        TradeType TradeType { get; set; }
    }

    public class BaseTrade:ITradePrice
    {
        public DateTime DateTime { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }

        public TradeType TradeType { get; set; }
    }

    public enum TradeType
    {
        Buy, Sell
    }
}
