using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface IBalanceRepository : IService
    {
        Task Add(string platform, string pair, decimal volume, decimal price);
        Task Remove(string platform, string pair);
        Task<List<BalanceItem>> Get(string platform, string pair);
    }

    public class BalanceRepository : BaseRepository, IBalanceRepository
    {
        public async Task Add(string platform, string pair, decimal volume, decimal price)
        {
            await Execute(async cmd =>
            {
                cmd.CommandText =
                    "INSERT INTO balance (platform, name, volume, price) VALUES (@platform, @pair, @volume, @price)";

                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@pair", pair);
                cmd.Parameters.AddWithValue("@volume", volume);
                cmd.Parameters.AddWithValue("@price", price);

                await cmd.ExecuteNonQueryAsync();
            });
        }

        public async Task Remove(string platform, string pair)
        {
            await Execute(async cmd =>
            {
                cmd.CommandText =
                    "update balance set isDeleted=1 where platform=@platform and name=@pair";

                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@pair", pair);

                await cmd.ExecuteNonQueryAsync();
            });
        }

        public async Task<List<BalanceItem>> Get(string platform, string pair)
        {
            var result = new List<BalanceItem>();

            await Execute(async cmd =>
            {
                cmd.CommandText =
                    @"select volume, price, notSoldCounter, notSoldDate, boughtDate from balance where platform=@platform and name=@pair and isDeleted=0";

                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@pair", pair);

                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {

                    result.Add(new BalanceItem
                    {
                        Volume = reader.GetDecimal(0),
                        Price = reader.GetDecimal(1),
                        NotSold = reader.GetInt32(2),
                        NotSoldtDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                        BoughtDate = reader.GetDateTime(4),
                    });
                }

            });
            return result;
        }
    }
}
