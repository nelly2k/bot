using System.Collections.Generic;

namespace bot.model
{
    public class Config:IApiCredentials
    {
        public Dictionary<string, PairConfig> Pairs { get; set; } = new Dictionary<string, PairConfig>();

        [Field("base currency")]
        public string BaseCurrency { get; set; } = "ZUSD";
        [Field("load interval minutes")]
        public int LoadIntervalMinutes { get; set; } = 3;
        [Field("key")]
        public string Key{ get; set; }
        [Field("secret")]
        public string Secret{ get; set; }

        public PairConfig this[string key]
        {
            get
            {
                if (!Pairs.ContainsKey(key))
                {
                    Pairs.Add(key, new PairConfig());
                }
                return Pairs[key];
            }
        }
       
    }

    public class PairConfig
    {
        [Field("is load")]
        public bool Load { get; set; } = true;

        [Field("is trade")]
        public bool Trade { get; set; } = false;
        [Field("is market")]
        public bool IsMarket { get; set; } = true;

        [Field("load hours")]
        public int LoadHours { get; set; } = 12;
        [Field("group by period minutes")]
        public int GroupMinutes { get; set; } = 4;
        [Field("group for long macd")]
        public int GroupForLongMacdMinutes { get; set; } = 20;
        [Field("short threshold minutes")]
        public int ThresholdMinutes { get; set; } = 30;
        [Field("macd short slow")]
        public int MacdShortSlow { get; set; } = 26;
        [Field("macd short fast")]
        public int MacdShortFast { get; set; } = 12;
        [Field("macd short signal")]
        public int MacdSignal { get; set; } = 9;
        [Field("rsi ema")]
        public int RsiEmaPeriods { get; set; } = 14;
        [Field("rsi low")]
        public int RsiLow { get; set; } = 30;
        [Field("rsi high")]
        public int RsiHigh { get; set; } = 70;
        [Field("max missed sells")]
        public int MaxMissedSells { get; set; } = 3;
        [Field("min volume")]
        public decimal MinVolume { get; set; } = 0.02m;
        [Field("price format")]
        public int PriceFormat { get; set; } = 2;
        [Field("share")]
        public int Share { get; set; } = 95;
    }
}
