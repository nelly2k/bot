using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface IOperationRepository:IService
    {
        Task<int> Add(string platform, string title, string pair, string misc);
        Task Complete(int id);
        Task<List<OperationItem>> GetIncomplete(string platform, string title);
    }

    public class OperationRepository : BaseRepository, IOperationRepository
    {
        public async Task<int> Add(string platform, string title, string pair, string misc)
        {
            var result = 0;
            await Execute(async cmd =>
            {
                cmd.CommandText = @"insert into operation (platform, title, pair, misc)
                    values (@platform, @title, @pair, @misc)
                    select @@IDENTITY";

                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@pair", pair);
                cmd.Parameters.AddWithValue("@misc", misc);
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

        public async Task<List<OperationItem>> GetIncomplete(string platform, string title)
        {
            var result = new List<OperationItem>();
            await Execute(async cmd =>
            {
                cmd.CommandText = @"select id, pair, misc from operation where platform = @platform and title = @title and isDeleted = 0";
                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@title", title);
                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    result.Add(new OperationItem
                    {
                        Id= reader.GetInt32(0),
                        Pair = reader.GetString(1),
                        Misc = reader.GetString(2),
                    });
                }
            });

            return result;
        }
        
    }

    public class OperationItem
    {
        public int Id { get; set; }
        public string Pair { get; set; }
        public string Misc { get; set; }
        
    }
}