using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace bot.kraken
{
    public class KrakenClient
    {
        private const string VERSION = "0";
        private const string PUBLIC = "public";

        public async Task<ServerTime> GetServerTime()
        {
            return  await CallPublic<ServerTime>("Time");
        }

        public async Task<Dictionary<string, Asset>> GetAssetInfo(string assetClass= "currency", params string[] assets)
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

        public async Task<SinceResponse<Trade>> GetTrades(string lastId=null, params string[] pairs)
        {
            var paramPairs = new Dictionary<string, string>()
                .AddParam("since", lastId)
                .AddParam("pair", pairs);

            var response = await CallPublic<Dictionary<string, object>>("Trades", paramPairs);

            var result = new SinceResponse<Trade>();
            result.Results = new List<Trade>();
            
            result.LastId = response.Last().Value.ToString();

           
            foreach (var tradesPair in response.Take(response.Count - 1))
            {
                foreach (var arr in tradesPair.Value as JArray)
                {
                    var trade = new Trade();
                    trade.PairName = tradesPair.Key;
                    trade.Price = (decimal)arr[0];
                    trade.Volume = (decimal)arr[1];
                    trade.DateTime = UnixTimeStampToDateTime((double)arr[2]);
                    trade.TransactionType = arr[3].ToString() == "b" ? TransactionType.Buy : TransactionType.Sell;
                    trade.PriceType = arr[4].ToString() == "m" ? PriceType.Market : PriceType.Limit;
                    trade.Misc = arr[5].ToString();
                    result.Results.Add(trade);
                }

            }

            return  result;
        }

        private async Task<TResult> CallPublic<TResult>(string url, Dictionary<string, string> paramPairs = null)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            var response = await client.GetAsync(BuildPublicPath(url, paramPairs));
            if (!response.IsSuccessStatusCode) throw new Exception($"Request is unsuccessful.");

            var serverResponse= await response.Content.ReadAsAsync<KrakenResponse<TResult>>();
            return serverResponse.Result;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public string BuildPublicPath(string path, Dictionary<string, string> paramPairs=null)
        {
            var builder = new UriBuilder
            {
                Scheme = Uri.UriSchemeHttps,
                Host = "api.kraken.com",
                Path = $"/{VERSION}/{PUBLIC}/{path}"
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

            return builder.Uri.ToString();
        }
    }
}


