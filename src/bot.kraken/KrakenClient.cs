using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace bot.kraken
{
    public class KrakenClient
    {
        private readonly IKrakenCredentials _credentials;
        private const string VERSION = "0";
        private const string PUBLIC = "public";
        private const string PRIVATE = "private";

        public KrakenClient()
        {
            
        }

        public KrakenClient(IKrakenCredentials credentials)
        {
            _credentials = credentials;
        }

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

        public async Task<Dictionary<string, object>> GetTrades(params string[] pairs)
        {
            var paramPairs = new Dictionary<string, string>()
                .AddParam("pair", pairs);

            return await CallPublic<Dictionary<string, object>>("Trades", paramPairs);
        }

        private async Task<TResult> CallPublic<TResult>(string url, Dictionary<string, string> paramPairs = null)
        {
            var client = new HttpClient();
            
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.GetAsync(BuildPath(url, true,paramPairs).ToString());
            if (!response.IsSuccessStatusCode) throw new Exception($"Request is unsuccessful.");

            var serverResponse= await response.Content.ReadAsAsync<KrakenResponse<TResult>>();
            return serverResponse.Result;
        }

        //Task<TResult>
        public async Task<TResult> CallPrivate<TResult>(string url, Dictionary<string, string> paramPairs = null)
        {
            if (_credentials==null)
            {
                throw  new CredentialsInvalidException();
            }
            var client = new HttpClient();
            Int64 nonce = DateTime.Now.Ticks;
            var props = $"nonce={nonce}";
     
             var uri = BuildPath(url, false, paramPairs);
         
            var content = new StringContent(props, Encoding.UTF8, "application/x-www-form-urlencoded");

            client.DefaultRequestHeaders.Add("API-Key", _credentials.Key);
            client.DefaultRequestHeaders.Add("API-Sign", Signature(nonce,props, uri));

            var response = await client.PostAsync(uri, content);
            if (!response.IsSuccessStatusCode) throw new Exception($"Request is unsuccessful.");

            var serverResponse = await response.Content.ReadAsAsync<KrakenResponse<TResult>>();
            return serverResponse.Result;
        }

        private string Signature(Int64 nonce,string props, Uri uri)
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

        public Uri BaseUrl()
        {
            var builder = new UriBuilder
            {
                Host = "api.kraken.com",
                Scheme = Uri.UriSchemeHttps,
            };
            return builder.Uri;
        }

        public Uri BuildPath(string path, bool isPublic = true, Dictionary<string, string> paramPairs=null)
        {

            var builder = new UriBuilder
            {
                Host = "api.kraken.com",
                Scheme = Uri.UriSchemeHttps,
                Path = $"/{VERSION}/{(isPublic?PUBLIC:PRIVATE)}/{path}"
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


