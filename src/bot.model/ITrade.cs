using System;

namespace bot.model
{
    public interface ITrade
    {
        string PairName { get; set; }
        decimal Price { get; set; }
        decimal Volume { get; set; }
        TradeType TradeType { get; set; }
        DateTime DateTime { get; set; }
     

        string Misc { get; set; }
    }

    public class BaseTrade:ITrade
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
