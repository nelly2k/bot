using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using bot.kraken.Model;
using bot.model;

namespace bot.kraken
{
    public class KrakenRepository : IKrakenRepository
    {
        private readonly Config _config;

        private const string VERSION = "0";
        private const string PUBLIC = "public";
        private const string PRIVATE = "private";


        public KrakenRepository(Config config)
        {
            _config = config;
        }

        public async Task<TResult> CallPublic<TResult>(string url, Dictionary<string, string> paramPairs = null)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.GetAsync(BuildPath(url, true, paramPairs).ToString());
            if (!response.IsSuccessStatusCode)
            {
                throw new InternalException($"Request is unsuccessful. [code:{response.StatusCode.ToString()}] [reason:{response.ReasonPhrase}]");
            }

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

        public  DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        private Uri BuildPublicPath(string path, Dictionary<string, string> paramPairs = null)
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

        private Uri BuildPath(string path, bool isPublic = true, Dictionary<string, string> paramPairs = null)
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