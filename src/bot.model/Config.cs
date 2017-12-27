using System.Collections.Generic;

namespace bot.model
{
    public interface IConfig
    {
        
    }
    public class Config:IApiCredentials, IConfig
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

        [Field("log prefix")]
        public string LogPrefix { get; set; } = "bot";

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

    public class PairConfig: IConfig
    {
        /// <summary>
        /// Going to load trading data from platform to database
        /// </summary>
        [Field("is load")]
        public bool ShouldLoad { get; set; } = true;

        /// <summary>
        /// Going to trade
        /// </summary>
        [Field("is trade")]
        public bool ShouldTrade { get; set; } = false;

        /// <summary>
        /// Going to trade on market price
        /// </summary>
        [Field("is market")]
        public bool IsMarket { get; set; } = true;

        /// <summary>
        /// How many hours of trading data is loaded for analysis
        /// p.s. this information could be calculated
        /// </summary>
        [Field("load hours")]
        public int LoadHours { get; set; } = 12;

        /// <summary>
        /// Period of grouping, probably one of the most important parameters
        /// </summary>
        [Field("group by period minutes")]
        public int GroupMinutes { get; set; } = 3;

        /// <summary>
        /// Second important grouping parameter
        /// Used to predict long price fluctuations
        /// </summary>
        [Field("group for long macd")]
        public int GroupForLongMacdMinutes { get; set; } = 20;

        /// <summary>
        /// Going to consider indicators signals within this window
        /// </summary>
        [Field("short threshold minutes")]
        public int ThresholdMinutes { get; set; } = 25;

        /// <summary>
        /// ETA 
        /// </summary>
        [Field("macd short slow")]
        public int MacdSlow { get; set; } = 20;
        [Field("macd short fast")]
        public int MacdFast { get; set; } = 10;
        [Field("macd short signal")]
        public int MacdSignal { get; set; } = 5;
        [Field("rsi ema")]
        public int RsiEmaPeriods { get; set; } = 16;
        [Field("rsi low")]
        public int RsiLow { get; set; } = 35;
        [Field("rsi high")]
        public int RsiHigh { get; set; } = 65;
        [Field("max missed sells")]
        public int MaxMissedSells { get; set; } = 4;
        [Field("min volume")]
        public decimal MinVolume { get; set; } = 0.02m;
        /// <summary>
        /// Number of digits after dot for price
        /// pair_decimal
        /// </summary>
        [Field("price format")]
        public int PriceFormat { get; set; } = 2;

        /// <summary>
        /// Number of digits after dot for volume
        /// lot_decimals
        /// </summary>
        [Field("volume format")]
        public int VolumeFormat { get; set; } = 8;

        /// <summary>
        /// Going to take this percent of USD to trade
        /// </summary>
        [Field("share")]
        public int Share { get; set; } = 95;

        public Dictionary<string, object> PlatformVariables { get; set; }

        public override string ToString()
        {
            return $"[group: {GroupMinutes}] [threshold:{ThresholdMinutes}] [group long: {GroupForLongMacdMinutes}]";
        }
    }
}
