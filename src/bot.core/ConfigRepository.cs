using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public class ConfigRepository : BaseRepository, IConfigRepository
    {
        private const string SHARED_CONFIG = "shared";

        private readonly IExchangeConfig[] _exchangeConfig;

        public ConfigRepository(IExchangeConfig[] exchangeConfig)
        {
            _exchangeConfig = exchangeConfig;
        }
        public async Task<Config> Get(string platform = "kraken")
        {
            var config = new Config();
            var clientConfig = _exchangeConfig.FirstOrDefault(x => x.Platform == platform);
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
                        CreatePair(config, reader.GetString(0), clientConfig);
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

        private void CreatePair(Config config, string pair, IExchangeConfig client)
        {
            if (config.Pairs.ContainsKey(pair))
            {
                return;
            }
            var newConfig = new PairConfig {PlatformVariables = new Dictionary<string, object>()};
            if (client != null)
            {
                foreach (var pairVariable in client.PairVariables)
                {
                    newConfig.PlatformVariables.Add(pairVariable.Key, pairVariable.Value);
                }
            }
            config.Pairs.Add(pair, newConfig);
        }

        private void SetPair(Config config, string pair, string field, object value)
        {
            if (config[pair].HasField(field))
            {
                config[pair].SetField(field, value);
            }
            else if (config[pair].PlatformVariables.ContainsKey(field))
            {
                config[pair].PlatformVariables[field] = value;
            }
        }

        public async Task Deploy(string platform, params string[] pairs)
        {
            await Deploy(typeof(Config), platform, SHARED_CONFIG);
            foreach (var pair in pairs)
            {
                await Deploy(typeof(PairConfig), platform, pair);
                await DeployPaltformPairVariables(platform, pair);
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
                await InsertIfNotExists(platform, pair, baseAttribute.GetField(), baseAttribute.GetValue(config));
            }
        }

        private async Task DeployPaltformPairVariables(string platform, string pair)
        {
            var clientConfig = _exchangeConfig.FirstOrDefault(x => x.Platform == platform);
            if (clientConfig == null)
            {
                return;
            }

            foreach (var pairVariable in clientConfig.PairVariables)
            {
                await InsertIfNotExists(platform, pair, pairVariable.Key, pairVariable.Value);
            }
        }

        public async Task CleanUp(string platform)
        {
            var allItems = await GetAllItems(platform);

            foreach (var item in allItems)
            {
                if (!IsRecordValid(item.Key, item.Value))
                {
                    await Remove(platform, item.Key, item.Value);
                }
            }
        }

        private async Task<List<KeyValuePair<string, string>>> GetAllItems(string platform)
        {
            var items = new List<KeyValuePair<string, string>>();

            await Execute(async cmd =>
            {
                cmd.CommandText = @"select pair, name from config where platform=@platform";
                cmd.Parameters.AddWithValue("@platform", platform);
                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    items.Add(new KeyValuePair<string, string>(reader.GetString(0), reader.GetString(1)));
                }
            });
            return items;
        }

        private bool IsRecordValid(string pair, string name)
        {
            if (pair == SHARED_CONFIG)
            {
                return IsNameExists(typeof(Config), name);
            }
            else
            {
                return IsNameExists(typeof(PairConfig), name);
            }
        }

        private bool IsNameExists(Type type, string name)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(p => p.GetCustomAttributes(typeof(FieldAttribute)).Any(x => ((FieldAttribute)x).Title == name));
        }

        private async Task Remove(string platform, string pair, string name)
        {
            await Execute(async cmd =>
            {
                cmd.CommandText = @"delete config
                                    where platform=@platform
	                                    and pair=@pair
	                                    and name=@name";
                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@pair", pair);
                cmd.Parameters.AddWithValue("@name", name);

                await cmd.ExecuteNonQueryAsync();
            });
        }

        private async Task InsertIfNotExists(string platform, string pair, string name, object value)
        {
            await Execute(async cmd =>
            {
                cmd.CommandText = @"if not exists (select * from config 
	                                    where platform=@platform
	                                    and pair=@pair
	                                    and name=@name)
                                    insert into config (platform, pair, name, value)
                                    values (@platform, @pair, @name,@value)";

                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@pair", pair);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@value", value??string.Empty);
                await cmd.ExecuteNonQueryAsync();
            });
        }

    }
}
