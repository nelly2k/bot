using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface IConfigRepository : IService
    {
        Task<Config> Get(string platform = "kraken");
    }

    public class ConfigRepository : BaseRepository, IConfigRepository
    {
        private const string SHARED_CONFIG = "shared";

        public async Task<Config> Get(string platform = "kraken")
        {
            var config = new Config();
            await Execute(async cmd =>
            {
                cmd.CommandText = @"select pair, name, value from config where platform=@platform";
                cmd.Parameters.AddWithValue("@platform", platform);
                var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    if (reader.GetString(0) == SHARED_CONFIG)
                    {
                        SetConfig(config, reader.GetString(1), reader[2]);
                    }
                    else
                    {
                        SetPair(config, reader.GetString(0), reader.GetString(1), reader[2]);
                    }
                }
            });
            return config;
        }

        private void SetConfig(Config config, string field, object value)
        {
            config.SetField(field, value);
        }

        private void SetPair(Config config, string pair, string field, object value)
        {
            if (!config.Pairs.ContainsKey(pair))
            {
                config.Pairs.Add(pair, new PairConfig());
            }
            config.Pairs[pair].SetField(field, value);
        }

        public async Task Deploy(string platform, params string[] pairs)
        {
            await Deploy(typeof(Config), platform, SHARED_CONFIG);
            foreach (var pair in pairs)
            {
                await Deploy(typeof(PairConfig), platform, pair);
            }
        }

        private async Task Deploy(Type type, string platform, string pair)
        {
            var config = Activator.CreateInstance(type);

            var baseAttributes = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                    p.GetCustomAttributes(typeof(FieldAttribute)).Any()
                    && (p.PropertyType == typeof(string)
                        || !typeof(IEnumerable).IsAssignableFrom(p.PropertyType)));

            foreach (var baseAttribute in baseAttributes)
            {
                await InsertIfNotExists(platform, pair, baseAttribute.GetField(), baseAttribute.GetValue(config).ToString());
            }

        }

        private async Task InsertIfNotExists(string pltaform, string pair, string name, string value)
        {
            await Execute(async cmd =>
            {
                cmd.CommandText = @"if not exists (select * from config 
	                                    where platform=@platform
	                                    and pair=@pair
	                                    and name=@name)
                                    insert into config (platform, pair, name, value)
                                    values (@platform, @pair, @name,@value)";

                cmd.Parameters.AddWithValue("@platform", pltaform);
                cmd.Parameters.AddWithValue("@pair", pair);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@value", value);
                await cmd.ExecuteNonQueryAsync();
            });
        }

    }
}
