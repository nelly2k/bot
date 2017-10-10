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

namespace bot.kraken
{
    public interface IKrakenClientService:IService, IExchangeClient
    {
        Task<ServerTime> GetServerTime();
        Task<Dictionary<string, Asset>> GetAssetInfo(string assetClass = "currency", params string[] assets);
        Task<Dictionary<string, AssetPair>> GetTradableAssetPairs(params string[] pairs);
        Task AddOrder(OrderType orderType, decimal volume);
        Task<List<OrderInfo>> GetClosedOrders();
        List<OrderInfo> ToOrders(Dictionary<string, object> orders);
        Task<List<OrderInfo>> GetOrdersInfo(params string[] orderIds);
        Task<Dictionary<string, decimal>> GetBalance();
        Task<TResult> CallPrivate<TResult>(string url, Dictionary<string, string> paramPairs = null);
        Uri BuildPublicPath(string path, Dictionary<string, string> paramPairs = null);
        Uri BuildPath(string path, bool isPublic = true, Dictionary<string, string> paramPairs = null);
    }

    public class KrakenClientService : IKrakenClientService
    {
        private readonly IApiCredentials _credentials;

        private const string VERSION = "0";
        private const string PUBLIC = "public";
        private const string PRIVATE = "private";

        public KrakenClientService(IApiCredentials credentials)
        {
            _credentials = credentials;
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

      
        public async Task<SinceResponse<ITrade>> GetTrades(string lastId = null, params string[] pairs)
        {
            var paramPairs = new Dictionary<string, string>()
                .AddParam("since", lastId)
                .AddParam("pair", pairs);

            var response = await CallPublic<Dictionary<string, object>>("Trades", paramPairs);

            var result = new SinceResponse<ITrade> {Results = new List<ITrade>()};
            if (response==null || !response.Any()) return result;

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
                    result.Results.Add(new KrakenTrade
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

        public async Task AddOrder(OrderType orderType, decimal volume)
        {
            var response = await CallPrivate<Dictionary<string, object>>("AddOrder", new Dictionary<string, string>
            {
                {"pair","ETHUSD"},
                {"type",orderType.ToString()},
                {"ordertype","market"},
                {"volume",volume.ToString(CultureInfo.InvariantCulture) }
            });
            foreach (var o in response)
            {
                Console.WriteLine(o.Key);
                Console.WriteLine(o.Value);
            }
            
        }

        public async Task<List<OrderInfo>> GetClosedOrders()
        {
            var paramPairs = new Dictionary<string, string>();
            var response = await CallPrivate<Dictionary<string, object>>("ClosedOrders", paramPairs);

            var result = new List<OrderInfo>();

            foreach (var closed in response.Take(response.Count - 1))
            {
                var orders = JsonConvert.DeserializeObject<Dictionary<string, object>>(closed.Value.ToString());
                result.AddRange(ToOrders(orders));
            }
            return result;
        }

        public List<OrderInfo> ToOrders(Dictionary<string, object> orders)
        {
            var result = new List<OrderInfo>();

            foreach (var order in orders)
            {
                var details = JsonConvert.DeserializeObject<Dictionary<string, object>>(order.Value.ToString());
                var desc = JsonConvert.DeserializeObject<Dictionary<string, object>>(details["descr"].ToString());

                var item = new OrderInfo();
                item.Id = order.Key;
                item.Status = Convert.ToString(details["status"]).ToEnum<OrderStatus>();
                item.Reason = Convert.ToString(details["reason"]);
                item.Pair = Convert.ToString(desc["pair"]);
                item.OrderType = Convert.ToString(desc["type"]).ToEnum<OrderType>();
                item.OrderPriceType = Convert.ToString(desc["ordertype"]).ToEnum<OrderPriceType>();
                item.PrimaryPrice = Convert.ToDecimal(desc["price"]);
                item.SecondaryPrice = Convert.ToDecimal(desc["price2"]);
                item.Leverage = Convert.ToString(desc["leverage"]);
                item.Volume = Convert.ToDecimal(details["vol"]);
                item.VolumeExec = Convert.ToDecimal(details["vol_exec"]);
                item.Cost = Convert.ToDecimal(details["cost"]);
                item.Fee = Convert.ToDecimal(details["fee"]);
                item.Price = Convert.ToDecimal(details["price"]);
                item.Misc = Convert.ToString(details["misc"]);
                result.Add(item);
            }
            return result;
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
            return serverResponse.Result;
        }

        public async Task<TResult> CallPrivate<TResult>(string url, Dictionary<string, string> paramPairs = null)
        {
            if (_credentials == null)
            {
                throw new CredentialsInvalidException();
            }
            var client = new HttpClient();
            Int64 nonce = DateTime.Now.Ticks;

            var uri = BuildPath(url, false);
            var props = $"nonce={nonce}{Porps(paramPairs)}";

            var content = new StringContent(props, Encoding.UTF8, "application/x-www-form-urlencoded");

            client.DefaultRequestHeaders.Add("API-Key", _credentials.Key);
            client.DefaultRequestHeaders.Add("API-Sign", Signature(nonce, props, uri));

            var response = await client.PostAsync(uri, content);
            if (!response.IsSuccessStatusCode) throw new Exception($"Request is unsuccessful.");
            var str = await response.Content.ReadAsStringAsync();
            var serverResponse = await response.Content.ReadAsAsync<KrakenResponse<TResult>>();
            return serverResponse.Result;
        }

        private string Signature(Int64 nonce, string props, Uri uri)
        {
            var base64DecodedSecred =
                Convert.FromBase64String(
                   _credentials.Secret);

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
}


