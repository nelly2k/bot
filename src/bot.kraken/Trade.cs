using System;
using System.Collections.Generic;
using bot.model;

namespace bot.kraken
{

    public class SinceResponse<TResult>
    {
        public List<TResult> Results { get; set; }
        public string LastId { get; set; }
        
    }
    public class Trade:ITradePrice
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

    public enum TransactionType
    {
        Buy,
        Sell
    }

    public enum PriceType
    {
        Market,
        Limit
    }
}
