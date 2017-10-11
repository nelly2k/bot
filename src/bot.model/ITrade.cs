using System;

namespace bot.model
{
    public interface IDateCost
    {
        DateTime DateTime { get; set; }
        decimal Price { get; set; }
    }
    
    public interface ITrade: IDateCost
    {
        string PairName { get; set; }
        decimal Volume { get; set; }
        TradeType TradeType { get; set; }
        TransactionType TransactionType { get; set; }
        PriceType PriceType { get; set; }
        string Misc { get; set; }
    }

    public class BaseTrade:ITrade
    {
        public string PairName { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public TradeType TradeType { get; set; }
        public DateTime DateTime { get; set; }
        public TransactionType TransactionType { get; set; }
        public PriceType PriceType { get; set; }
        public string Misc { get; set; }
    }

    public enum TradeType
    {
        Buy, Sell
    }
}
