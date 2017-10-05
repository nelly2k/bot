using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public class DatabaseService
    {
        private string connectionString = "Server=(local);Database=bot;Trusted_Connection=True;";

        public async Task<IEnumerable<BaseTrade>> LoadTrades(string altname, DateTime since, DateTime? to = null)
        {
            using (var con = new SqlConnection(connectionString))
            {
                var sb = new StringBuilder();
                sb.Append(@" select tradeTime, price, volume, buy_sell from trades
                            where tradeTime > @dateTime");

                if (to.HasValue)
                {
                    sb.Append(" and tradeTime < @dateTo");
                }

                sb.Append(@" and altname = @altname
                                  order by tradeTime");
                
                con.Open();
                var result = new List<BaseTrade>();
                using (var command = new SqlCommand(sb.ToString(), con))
                {
                    command.Parameters.AddWithValue("@altname", altname);
                    if (to != null)
                    {
                        command.Parameters.AddWithValue("@dateTo", to);
                    }
                    command.Parameters.AddWithValue("@dateTime", since);

                    var reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        result.Add(new BaseTrade
                        {
                            DateTime = reader.GetDateTime(0),
                            Price = reader.GetDecimal(1),
                            Volume = reader.GetDecimal(2),
                            TradeType = reader.GetString(3)=="b"?TradeType.Buy:TradeType.Sell,
                        });
                    }
                    con.Close();
                    return result;
                }

            }
        }

        public async Task Log(string platform, string status, string what)
        {
            using (var con = new SqlConnection(connectionString))
            {
                var text = @"INSERT INTO [dbo].[log]
           ([platform]
           ,[status]
           ,[event])
     VALUES (@platform,@status,@event)";
                con.Open();
                using (var com = new SqlCommand(text, con))
                {
                    com.Parameters.AddWithValue("@platform", "kraken");
                    com.Parameters.AddWithValue("@status", status);
                    com.Parameters.AddWithValue("@event", what);
                    await com.ExecuteNonQueryAsync();
                    con.Close();
                }
            }
        }

        private readonly Dictionary<string, Action<Config, object>> _dict = new Dictionary<string, Action<Config, object>>
        {
            {"load_interval_minutes", (c, v) => c.LoadIntervalMinutes = Convert.ToInt32(v)},
            {"analyse_load_hours", (c, v) => c.AnalyseLoadHours = Convert.ToInt32(v)},
            {"analyse_group_period_minutes", (c, v) => c.AnalyseGroupPeriodMinutes = Convert.ToInt32(v)},
            {"analyse_treshold_minutes", (c, v) => c.AnalyseTresholdMinutes = Convert.ToInt32(v)},
            {"analyse_macd_slow", (c, v) => c.AnalyseMacdSlow = Convert.ToInt32(v)},
            {"analyse_macd_fast", (c, v) => c.AnalyseMacdFast = Convert.ToInt32(v)},
            {"analyse_macd_signal", (c, v) => c.AnalyseMacdSignal = Convert.ToInt32(v)},
            {"analyse_rsi_ema_periods", (c, v) => c.AnalyseRsiEmaPeriods = Convert.ToInt32(v)},
            {"analyse_rsi_low", (c, v) => c.AnalyseRsiLow = Convert.ToInt32(v)},
            {"analyse_rsi_high", (c, v) => c.AnalyseRsiHigh = Convert.ToInt32(v)},
        };

        public async Task<Config> GetConfig()
        {
            using (var con = new SqlConnection(connectionString))
            {
                var commandText = @"select name, value from config where platform='kraken'";
                con.Open();

                using (var command = new SqlCommand(commandText, con))
                {
                    var reader = await command.ExecuteReaderAsync();
                    var config = new Config();
                    while (reader.Read())
                    {
                        var key = reader[0].ToString();
                        if (_dict.ContainsKey(key))
                        {
                            _dict[key](config, reader[1]);
                        }
                    }
                    con.Close();
                    return config;
                }
            }
        }
    }
}
