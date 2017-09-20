using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace bot.kraken
{
    public class KrakenClient
    {
        private const string URL = "api.kraken.com";
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

        private async Task<TResult> CallPublic<TResult>(string url, Dictionary<string, string> paramPairs = null)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            var response = await client.GetAsync(BuildPublicPath(url, paramPairs));
            if (response.IsSuccessStatusCode)
            {
                var serverResponse= await response.Content.ReadAsAsync<KrakenResponse<TResult>>();
                return serverResponse.Result;
            }
            else
            {
                throw new Exception($"Request is unsuccessful.");
            }

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


