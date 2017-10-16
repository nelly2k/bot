using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface IDatabaseService:IService
    {
        Task<IEnumerable<BaseTrade>> LoadTrades(string altname, DateTime since, DateTime? to = null);
        Task SaveTrades(List<ITrade> trades);
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            _connectionString=ConfigurationManager.AppSettings["db"];
        }

        public async Task<IEnumerable<BaseTrade>> LoadTrades(string altname, DateTime since, DateTime? to = null)
        {
            using (var con = new SqlConnection(_connectionString))
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
            using (var con = new SqlConnection(_connectionString))
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

        public async Task SaveTrades(List<ITrade> trades)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                var commandText = @"INSERT INTO [dbo].[Trades]
           ([altname]
           ,[price]
           ,[volume]
           ,[tradeTime]
           ,[buy_sell]
           ,[market_limit]
           ,[misc])
     VALUES
           (@altname
           ,@price
           ,@volume
           ,@tradeTime
           ,@buy_sell
           ,@market_limit
           ,@misc)";
                con.Open();
                foreach (var trade in trades)
                {
                    using (var command = new SqlCommand(commandText, con))
                    {
                        command.Parameters.AddWithValue("@altname", trade.PairName);
                        command.Parameters.AddWithValue("@price", trade.Price);
                        command.Parameters.AddWithValue("@volume", trade.Volume);
                        command.Parameters.AddWithValue("@tradeTime", trade.DateTime);
                        command.Parameters.AddWithValue("@buy_sell", trade.TransactionType == TransactionType.Buy ? "b" : "s");
                        command.Parameters.AddWithValue("@market_limit", trade.PriceType == PriceType.Limit ? "l" : "m");
                        command.Parameters.AddWithValue("@misc", trade.Misc);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                con.Close();
            }

        }


    }
    
}
