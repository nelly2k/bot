using System;
using bot.model;

namespace bot.kraken.Model
{
    public class KrakenTrade:ITrade
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
}