using System;
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
        public async Task GetTrades()
        {
            var cr = new KrakenClient();
            var result = await cr.GetTrades(pairs:"ETHUSD");
            Assert.That(result.LastId, Is.Not.Null);
            Assert.That(result.Results, Is.Not.Null);
        }

        [Test]
        public void BuildUrl()
        {
            var cr = new KrakenClient();
            var url = cr.BuildPublicPath("Asset", new Dictionary<string, string>() {{"asset", "ETH"}});

            Assert.That(url, Is.EqualTo("https://api.kraken.com/0/public/Asset?asset=ETH"));

        }

        [Test]
        public async Task AddTradeToDb()
        {
            var trade = new Trade()
            {
                PairName = "TEST",
                Price = (decimal)12.0005,
                Volume = (decimal)4.0005,
                TransactionType = TransactionType.Buy,
                PriceType = PriceType.Market,
                Misc = "TEST transaction",
                DateTime = new DateTime(2015,5,15)
            };
            var d= new DatabaseService();
            await d.Save(new List<Trade>(){trade});
        }

        [Test]
        public async Task SaveLastId()
        {
            var d = new DatabaseService();
            await d.SaveLastId("TEST", "3216549848454564545");
            Assert.That(await d.GetId("TEST"), Is.EqualTo("3216549848454564545"));
            }

        [Test]
        public async Task GetLastId()
        {
            var d = new DatabaseService();
            var result = await d.GetId("TEST");
            Assert.That(result, Is.EqualTo("321"));
        }

        [Test]
        public async Task GetLastId_Not_exists()
        {
            var d = new DatabaseService();
            var result = await d.GetId("EXT");
            Assert.That(result, Is.Null);
        }

    }
}
