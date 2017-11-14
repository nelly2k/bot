using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface IConfigRepository:IService
    {
        Task<Config> Get(string platform = "kraken");
    }

    public class ConfigRepository:BaseRepository, IConfigRepository
    {
        //private readonly Dictionary<string, Action<Config, object>> _dict = new Dictionary<string, Action<Config, object>>
        //{
        //    {"load_interval_minutes", (c, v) => c.LoadIntervalMinutes = Convert.ToInt32(v)},
        //    {"analyse_load_hours", (c, v) => c.AnalyseLoadHours = Convert.ToInt32(v)},
        //    {"analyse_group_period_minutes", (c, v) => c.AnalyseGroupPeriodMinutes = Convert.ToInt32(v)},
        //    {"analyse_amcd_group_perios_minutes_slow", (c, v) => c.AnalyseMacdGroupPeriodMinutesSlow = Convert.ToInt32(v)},
        //    {"analyse_treshold_minutes", (c, v) => c.AnalyseTresholdMinutes = Convert.ToInt32(v)},
        //    {"analyse_macd_slow", (c, v) => c.AnalyseMacdSlow = Convert.ToInt32(v)},
        //    {"analyse_macd_fast", (c, v) => c.AnalyseMacdFast = Convert.ToInt32(v)},
        //    {"analyse_macd_signal", (c, v) => c.AnalyseMacdSignal = Convert.ToInt32(v)},
        //    {"analyse_rsi_ema_periods", (c, v) => c.AnalyseRsiEmaPeriods = Convert.ToInt32(v)},
        //    {"analyse_rsi_low", (c, v) => c.AnalyseRsiLow = Convert.ToInt32(v)},
        //    {"analyse_rsi_high", (c, v) => c.AnalyseRsiHigh = Convert.ToInt32(v)},
        //    {"base_currency", (c, v) => c.BaseCurrency = v.ToString()},
        //    {"is_market", (c, v) => c.IsMarket = Convert.ToBoolean(v)},
        //    {"analyse_macd_slow_threshold", (c, v) => c.AnalyseMacdSlowThreshold = Convert.ToDecimal(v)},
        //    { "pair_load", (c, v) =>
        //    {
        //        c.PairToLoad.Add(v.ToString());
        //    } },
        //    {"pair_percent", (c, v) =>
        //        {
        //            var p = v.ToString().Split('|');
        //            c.PairPercent.Add(p[0], Convert.ToDouble(p[1]));
        //        }
        //    },
        //    {"min_volume", (c, v) =>
        //        {
        //            var p = v.ToString().Split('|');
        //            c.MinVolume.Add(p[0], Convert.ToDecimal(p[1]));
        //        }
        //    },
        //    {"api_key", (c, v) => c.Key = v.ToString()},
        //    {"api_secret", (c, v) => c.Secret = v.ToString()},
        //};

        public async Task<Config> Get(string platform = "kraken")
        {
            var config = new Config();
            //await Execute(async cmd =>
            //{
            //    cmd.CommandText = @"select name, value from config where platform=@platform";
            //    cmd.Parameters.AddWithValue("@platform", platform);
            //    var reader = await cmd.ExecuteReaderAsync();
            //    while (reader.Read())
            //    {
            //        var key = reader[0].ToString();
            //        if (_dict.ContainsKey(key))
            //        {
            //            _dict[key](config, reader[1]);
            //        }
            //    }

            //});
            return config;
        }

        public async Task Deploy(params string[] pairs)
        {
            var conifg = new Config();


        }



    }
}
