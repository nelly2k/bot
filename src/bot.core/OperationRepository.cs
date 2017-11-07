using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace bot.core
{
    public class OperationRepository : BaseRepository
    {
        public async Task<int> Add(string platform, string title)
        {
            var result = 0;
            await Execute(async cmd =>
            {
                cmd.CommandText = @"insert into operation (platform, title)
                    values (@platform, @title)
                    select @@IDENTITY";

                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@title", title);
                result = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            });
            return result;
        }

        public async Task Complete(int id)
        {
            await Execute(async cmd =>
            {
                cmd.CommandText = @"update operation set isDeleted = 1 where id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
            });
        }

        public async Task<List<int>> GetIncomplete(string platform, string title)
        {
            var result = new List<int>();
            await Execute(async cmd =>
            {
                cmd.CommandText = @"select id from operation where platform = @platform and title = @title and isDeleted = 0";
                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@title", title);
                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    result.Add(reader.GetInt32(0));
                }
            });

            return result;
        }
    }
}