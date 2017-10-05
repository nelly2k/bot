using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace bot.kraken.test
{
    public class KrackenClientTests
    {
        [Test]
        public async Task GetServerTime()
        {
            var cr = new KrakenClient();
            var time = await cr.GetServerTime();
            Assert.That(time.Rfc1123, Is.Not.Null);
        }

        [Test]
        public async Task GetAssetInfo()
        {
            var cr = new KrakenClient();
            var result = await cr.GetAssetInfo();
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetEtherAssetInfo()
        {
            var cr = new KrakenClient();
            var result = await cr.GetAssetInfo(assets:"ETH");
            Assert.That(result.Values.First().Altname, Is.EqualTo("ETH"));
        }

        [Test]
        public async Task GetAssetPairs()
        {
            var cr = new KrakenClient();
            var result = await cr.GetTradableAssetPairs();
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async void TestName()
        {
            var cr = new KrakenClient();
            var result = await cr.GetTrades("ETHUSD");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildUrl()
        {
            var cr = new KrakenClient();
            var url = cr.BuildPath("Asset", true, new Dictionary<string, string>() {{"asset", "ETH"}});
            
            Assert.That(url.ToString(), Is.EqualTo("https://api.kraken.com/0/public/Asset?asset=ETH"));


        }

        [Test]
        public async Task GetBalance()
        {
            var testCredentials = new KrakenCredentials
            {
                Secret = "oyk16w/LzydxZ4aAnuy2DK5qzByYG9ja1XaW6BQ9uP1hongaczewuGUeAaWE5rHL5vvIrBEF2j13E97hvuPVmA==",
                Key = "eR55LkazbSk6AG27P/RgwvORohb2fgZpB2sKVpZOzJlWyRB27ttqgt1d"
            };
            var cr = new KrakenClient(testCredentials);
            await cr.CallPrivate<int>("Balance");
        }
    }
}
