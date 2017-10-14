using System.Collections.Generic;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface IOrderRepository:IService
    {
        Task Add(string platform, string pair, string orderId);
        Task Remove(string platform, string orderId);
        Task<List<string>> Get(string platform);
    }

    public class OrderRepository:BaseRepository, IOrderRepository
    {
        public async Task Add(string platform, string pair, string orderId)
        {
            await Execute(async cmd =>
            {
                cmd.CommandText =
                    "INSERT INTO openOrder (platform, altname, id) VALUES (@platform, @altname, @orderId)";

                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@altname", pair);
                cmd.Parameters.AddWithValue("@orderId", orderId);

                await cmd.ExecuteNonQueryAsync();
            });
        }

        public async Task Remove(string platform, string orderId)
        {
            await Execute(async cmd =>
            {
                cmd.CommandText =
                    "delete openOrder where platform = @platform and id = @orderId";

                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@orderId", orderId);

                await cmd.ExecuteNonQueryAsync();
            });
        }

        public async Task<List<string>> Get(string platform)
        {
            var result = new List<string>();
            await Execute(async cmd =>
            {
                cmd.CommandText =
                    "select id from openOrder where platform = @platform";

                cmd.Parameters.AddWithValue("@platform", platform);

                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    result.Add(reader[0].ToString());
                }
            });

            return result;
        }
    }


}
