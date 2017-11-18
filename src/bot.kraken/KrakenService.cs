using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using bot.kraken.Model;
using bot.model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrderStatus = bot.model.OrderStatus;

namespace bot.kraken
{
    public interface IKrakenClientService : IExchangeClient
    {
        Task<ServerTime> GetServerTime();
        Task<Dictionary<string, Asset>> GetAssetInfo(string assetClass = "currency", params string[] assets);
        Task<Dictionary<string, AssetPair>> GetTradableAssetPairs(params string[] pairs);
        Task<List<OrderInfo>> GetClosedOrders(int? userref = null);
        List<OrderInfo> ToOrders(Dictionary<string, object> orders);
        Task<List<OrderInfo>> GetOrdersInfo(params string[] orderIds);
        Task<Dictionary<string, decimal>> GetBalance();
        Task<TResult> CallPrivate<TResult>(string url, Dictionary<string, string> paramPairs = null);
        Uri BuildPublicPath(string path, Dictionary<string, string> paramPairs = null);
        Uri BuildPath(string path, bool isPublic = true, Dictionary<string, string> paramPairs = null);
    }

    public class KrakenClientService : IKrakenClientService
    {
        private readonly Config _config;

        private const string VERSION = "0";
        private const string PUBLIC = "public";
        private const string PRIVATE = "private";

        public KrakenClientService(Config config)
        {
            _config = config;
        }

        public async Task<ServerTime> GetServerTime()
        {
            return await CallPublic<ServerTime>("Time");
        }

        public async Task<Dictionary<string, Asset>> GetAssetInfo(string assetClass = "currency", params string[] assets)
        {
            var pairs = new Dictionary<string, string>()
                .AddParam("aclass", assetClass)
                .AddParam("asset", assets);
            return await CallPublic<Dictionary<string, Asset>>("Assets", pairs);
        }

        public async Task<Dictionary<string, AssetPair>> GetTradableAssetPairs(params string[] pairs)
        {
            var paramPairs = new Dictionary<string, string>()
                .AddParam("pair", pairs);

            return await CallPublic<Dictionary<string, AssetPair>>("AssetPairs", paramPairs);
        }


        public string Platform => "kraken";

        public async Task<SinceResponse<ITrade>> GetTrades(string lastId = null, params string[] pairs)
        {
            var paramPairs = new Dictionary<string, string>()
                .AddParam("since", lastId)
                .AddParam("pair", pairs);

            var response = await CallPublic<Dictionary<string, object>>("Trades", paramPairs);

            var result = new SinceResponse<ITrade> { Results = new List<ITrade>() };
            if (response == null || !response.Any()) return result;

            result.LastId = Convert.ToString(response.Last().Value);
            if (response.Count <= 1) return result;

            foreach (var tradesPair in response.Take(response.Count - 1))
            {
                var trades = tradesPair.Value as JArray;
                if (trades == null)
                {
                    continue;
                }
                foreach (var arr in trades)
                {
                    result.Results.Add(new BaseTrade
                    {
                        PairName = tradesPair.Key,
                        Price = (decimal)arr[0],
                        Volume = (decimal)arr[1],
                        DateTime = UnixTimeStampToDateTime((double)arr[2]),
                        TransactionType = arr[3].ToString() == "b" ? TransactionType.Buy : TransactionType.Sell,
                        PriceType = arr[4].ToString() == "m" ? PriceType.Market : PriceType.Limit,
                        Misc = arr[5].ToString()
                    });
                }
            }

            return result;
        }

        public async Task<List<string>> Buy(decimal volume, string pair, decimal? price=null, int? operationId=null)
        {
            return await AddOrder(OrderType.buy, price.HasValue ? "limit" : "market", volume, pair, price, operationId);
        }

        public async Task<List<string>> Sell(decimal volume, string pair, decimal? price = null, int? operationId = null)
        {
            return await AddOrder(OrderType.sell, price.HasValue ? "limit" : "market", volume, pair, price, operationId);
        }

        public async Task<List<string>> AddOrder(OrderType operationType, 
            string orderType, 
            decimal volume, 
            string pair, decimal? price = null, int? operationId = null, Dictionary<string,string> additionalParameters = null)
        {
            var pars = new Dictionary<string, string>
            {
                {"pair", pair},
                {"type", operationType.ToString()},
                {"ordertype", orderType},
                {"volume", volume.ToString(CultureInfo.InvariantCulture)}
            };
            if (price.HasValue)
            {
                pars.Add("price", price.ToString());
            }
            if (operationId.HasValue)
            {
                pars.Add("userref", operationId.ToString());
            }

            if (additionalParameters != null)
            {
                foreach (var parameter in additionalParameters)
                {
                    if (pars.ContainsKey(parameter.Key))
                    {
                        pars[parameter.Key] = parameter.Value;
                    }
                    else
                    {
                        pars.Add(parameter.Key, parameter.Value);
                    }
                }   
            }

            var response = await CallPrivate<Dictionary<string, object>>("AddOrder", pars);

            var txid = response["txid"];
            return JsonConvert.DeserializeObject<List<string>>(txid.ToString());

        }

        public async Task<List<string>> AddOrder(OrderType orderType, decimal volume, string pair, decimal? price = null, int? operationId = null)
        {
            var pars = new Dictionary<string, string>
            {
                {"pair", pair},
                {"type", orderType.ToString().Replace("_","-")},
                {"ordertype", price.HasValue ? "limit" : "market"},
                {"volume", volume.ToString(CultureInfo.InvariantCulture)}
            };
            if (price.HasValue)
            {
                pars.Add("price", price.ToString());
            }
            if (operationId.HasValue)
            {
                pars.Add("userref", operationId.ToString());
            }

            var response = await CallPrivate<Dictionary<string, object>>("AddOrder", pars);

            var txid = response["txid"];
            return JsonConvert.DeserializeObject<List<string>>(txid.ToString());

        }

        public List<OrderInfo> ToOrders(Dictionary<string, object> orders)
        {
            var result = new List<OrderInfo>();

            foreach (var order in orders)
            {
                var details = JsonConvert.DeserializeObject<Dictionary<string, object>>(order.Value.ToString());
                var item = new OrderInfo();
                item.Id = order.Key;
                if (details.ContainsKey("descr"))
                {
                    var desc = JsonConvert.DeserializeObject<Dictionary<string, object>>(details["descr"].ToString());
                    Set("pair", desc, o => item.Pair = Convert.ToString(o));
                    Set("type", desc, o => item.OrderType = Convert.ToString(o).ToEnum<OrderType>());
                    Set("ordertype", desc, o => item.OrderPriceType = Convert.ToString(o).ToEnum<OrderPriceType>());
                    Set("price", desc, o => item.PrimaryPrice = Convert.ToDecimal(o));
                    Set("price2", desc, o => item.SecondaryPrice = Convert.ToDecimal(o));
                    Set("leverage", desc, o => item.Leverage = Convert.ToString(o));
                }

                Set("status", details, o => item.Status = Convert.ToString(o).ToEnum<KrakenOrderStatus>());
                Set("reason", details, o => item.Reason = Convert.ToString(o));
                Set("userref", details, o => item.UserRef = Convert.ToString(o));

                Set("vol", details, o => item.Volume = Convert.ToDecimal(o));
                Set("vol_exec", details, o => item.VolumeExec = Convert.ToDecimal(o));
                Set("cost", details, o => item.Cost = Convert.ToDecimal(o));
                Set("price", details, o => item.Price = Convert.ToDecimal(o));
                Set("fee", details, o => item.Fee = Convert.ToDecimal(o));
                Set("misc", details, o => item.Misc = Convert.ToString(o));

                result.Add(item);
            }
            return result;
        }

        private void Set(string name, Dictionary<string, object> details, Action<object> setAction)
        {
            if (details.ContainsKey(name))
            {
                setAction(details[name]);
            }
        }

        public async Task<List<OrderInfo>> GetOrdersInfo(params string[] orderIds)
        {
            var paramPairs = new Dictionary<string, string>()
            {
                {"txid", string.Join(",", orderIds) }
            };
            var response = await CallPrivate<Dictionary<string, object>>("QueryOrders", paramPairs);
            return ToOrders(response);
        }

        public async Task<List<string>> GetOrdersIds(int userref)
        {
            var result = new List<string>();
            result.AddRange((await GetOpenOrders(userref)).Select(x=>x.Id));
            result.AddRange((await GetClosedOrders(userref)).Select(x => x.Id));
            return result;

        }

        public async Task<List<OrderInfo>> GetClosedOrders(int? userref = null)
        {
            var paramPairs = new Dictionary<string, string>();

            if (userref.HasValue)
            {
                paramPairs.Add("userref", userref.ToString());
            }

            var response = await CallPrivate<Dictionary<string, object>>("ClosedOrders", paramPairs);

            var result = new List<OrderInfo>();
            if (response.Count <= 1)
            {
                return new List<OrderInfo>();

            }
            foreach (var closed in response.Take(response.Count - 1))
            {
                var orders = JsonConvert.DeserializeObject<Dictionary<string, object>>(closed.Value.ToString());
                result.AddRange(ToOrders(orders));
            }
            return result;
        }


        public async Task<List<Order>> GetOpenOrders(int? userref = null)
        {
            var paramPairs = new Dictionary<string, string>();

            if (userref.HasValue)
            {
                paramPairs.Add("userref", userref.ToString());
            }
            var response = await CallPrivate<Dictionary<string, object>>("OpenOrders", paramPairs);
            if (response.Count <= 1)
            {
                return new List<Order>();

            }
            var orderInfos = ToOrders(response);
            return orderInfos.Select(x => new Order
            {
                Id = x.Id,
                OrderStatus = x.Status == KrakenOrderStatus.closed ? OrderStatus.Closed : OrderStatus.Pending,
                Volume = x.Volume,
                Price = x.Price,
                Pair = x.Pair,
                OrderType = x.OrderType
            }).ToList();
        }

        public async Task<List<Order>> GetOrders(params string[] refs)
        {
            var orderInfos = await GetOrdersInfo(refs);
            return orderInfos.Select(x => new Order
            {
                Id = x.Id,
                OrderStatus = x.Status == KrakenOrderStatus.closed ? OrderStatus.Closed : OrderStatus.Pending,
                Volume = x.Volume,
                Price = x.Price,
                Pair = x.Pair,
                OrderType = x.OrderType
            }).ToList();
        }

        public async Task<decimal> GetBaseCurrencyBalance()
        {
            var balance = await GetBalance();
            if (balance.ContainsKey(_config.BaseCurrency))
            {
                return balance[_config.BaseCurrency];
            }
            return decimal.Zero;
        }

        public async Task<Dictionary<string, decimal>> GetBalance()
        {
            return await CallPrivate<Dictionary<string, decimal>>("Balance");
        }

        private async Task<TResult> CallPublic<TResult>(string url, Dictionary<string, string> paramPairs = null)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.GetAsync(BuildPath(url, true, paramPairs).ToString());
            if (!response.IsSuccessStatusCode) throw new Exception($"Request is unsuccessful.");

            var serverResponse = await response.Content.ReadAsAsync<KrakenResponse<TResult>>();
            if (serverResponse.Error != null && serverResponse.Error.Any())
            {
                throw new Exception($"Kraken returned error: {string.Join(Environment.NewLine, serverResponse.Error)}");
            }
            return serverResponse.Result;
        }

        public async Task<TResult> CallPrivate<TResult>(string url, Dictionary<string, string> paramPairs = null)
        {
            if (_config == null)
            {
                throw new CredentialsInvalidException();
            }
            var client = new HttpClient();
            Int64 nonce = DateTime.Now.Ticks;

            var uri = BuildPath(url, false);
            var props = $"nonce={nonce}{Porps(paramPairs)}";

            var content = new StringContent(props, Encoding.UTF8, "application/x-www-form-urlencoded");

            client.DefaultRequestHeaders.Add("API-Key", _config.Key);
            client.DefaultRequestHeaders.Add("API-Sign", Signature(nonce, props, uri));

            var response = await client.PostAsync(uri, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new InternalException($"Request is unsuccessful. [code:{response.StatusCode.ToString()}] [reason:{response.ReasonPhrase}]");
            }

            //  var str = await response.Content.ReadAsStringAsync();
            var serverResponse = await response.Content.ReadAsAsync<KrakenResponse<TResult>>();
            if (serverResponse.Error != null && serverResponse.Error.Any())
            {
                throw new Exception($"Kraken returned error: {string.Join(Environment.NewLine, serverResponse.Error)}");
            }
            return serverResponse.Result;
        }

        private string Signature(Int64 nonce, string props, Uri uri)
        {
            var base64DecodedSecred =
                Convert.FromBase64String(
                   _config.Secret);

            var np = nonce + Convert.ToChar(0) + props;

            var pathBytes = Encoding.UTF8.GetBytes(uri.AbsolutePath);
            var hash256Bytes = sha256_hash(np);
            var z = new byte[pathBytes.Count() + hash256Bytes.Count()];
            pathBytes.CopyTo(z, 0);
            hash256Bytes.CopyTo(z, pathBytes.Count());

            var signature = getHash(base64DecodedSecred, z);
            return Convert.ToBase64String(signature);
        }

        private byte[] getHash(byte[] keyByte, byte[] messageBytes)
        {
            using (var hmacsha512 = new HMACSHA512(keyByte))
            {

                Byte[] result = hmacsha512.ComputeHash(messageBytes);

                return result;

            }
        }

        private byte[] sha256_hash(String value)
        {
            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;

                Byte[] result = hash.ComputeHash(enc.GetBytes(value));

                return result;
            }
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public Uri BuildPublicPath(string path, Dictionary<string, string> paramPairs = null)
        {
            var builder = new UriBuilder
            {
                Host = "api.kraken.com",
                Scheme = Uri.UriSchemeHttps,
            };
            return builder.Uri;
        }

        private string Porps(Dictionary<string, string> param)
        {
            return param != null ? $"&{string.Join("&", param.Select(x => $"{x.Key}={x.Value}"))}" : string.Empty;
        }

        public Uri BuildPath(string path, bool isPublic = true, Dictionary<string, string> paramPairs = null)
        {

            var builder = new UriBuilder
            {
                Host = "api.kraken.com",
                Scheme = Uri.UriSchemeHttps,
                Path = $"/{VERSION}/{(isPublic ? PUBLIC : PRIVATE)}/{path}"
            };

            if (paramPairs != null)
            {
                var query = HttpUtility.ParseQueryString(builder.Query);
                foreach (var pair in paramPairs)
                {
                    query.Add(pair.Key, pair.Value);
                }
                builder.Query = query.ToString();
            }

            return builder.Uri;
        }




    }

    public class InternalException : Exception
    {
        public InternalException(string message) : base(message)
        {

        }

    }

}


