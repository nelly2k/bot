using System.Collections.Generic;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface IOrderRepository:IService
    {
        Task Add(string platform, string pair, string orderId);
        Task Remove(string platform, string orderId);
        Task<Dictionary<string, string>> Get(string platform);
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
                    "update openOrder set isDeleted = 1 where platform = @platform and id = @orderId";

                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@orderId", orderId);

                await cmd.ExecuteNonQueryAsync();
            });
        }

        public async Task<Dictionary<string, string>> Get(string platform)
        {
            var result = new Dictionary<string, string>();
            await Execute(async cmd =>
            {
                cmd.CommandText =
                    "select id, altname from openOrder where platform = @platform and isDeleted=0";

                cmd.Parameters.AddWithValue("@platform", platform);

                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    result.Add(reader.GetString(0), reader.GetString(1));
                }
            });

            return result;
        }
    }


}
