using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface IBalanceRepository : IService
    {
        Task Add(string platform, string pair, decimal volume, decimal price, bool isBorrowed);
        Task Remove(string platform, string pair, bool isBorrowed);
        Task<List<BalanceItem>> Get(string platform, string pair);
    }

    public class BalanceRepository : BaseRepository, IBalanceRepository
    {
        public async Task Add(string platform, string pair, decimal volume, decimal price, bool isBorrowed)
        {
            await Execute(async cmd =>
            {
                cmd.CommandText =
                    "INSERT INTO balance (platform, name, volume, price, isBorrowed) VALUES (@platform, @pair, @volume, @price, @isBorrowed)";

                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@pair", pair);
                cmd.Parameters.AddWithValue("@volume", volume);
                cmd.Parameters.AddWithValue("@price", price);
                cmd.Parameters.AddWithValue("@isBorrowed", isBorrowed);

                await cmd.ExecuteNonQueryAsync();
            });
        }

        public async Task Remove(string platform, string pair, bool isBorrowed)
        {
            await Execute(async cmd =>
            {
                cmd.CommandText =
                    "update balance set isDeleted=1 where platform=@platform and name=@pair and isBorrowed=@isBorrowed";

                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@pair", pair);
                cmd.Parameters.AddWithValue("@isBorrowed", isBorrowed);

                await cmd.ExecuteNonQueryAsync();
            });
        }

        public async Task<List<BalanceItem>> Get(string platform, string pair)
        {
            var result = new List<BalanceItem>();

            await Execute(async cmd =>
            {
                cmd.CommandText =
                    @"select volume, price, notSoldCounter, notSoldDate, boughtDate, isBorrowed from balance where platform=@platform and name=@pair and isDeleted=0";

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
                        IsBorrowed = reader.GetBoolean(5)
                    });
                }

            });
            return result;
        }
    }
}
