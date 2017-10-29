using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Threading.Tasks;
using bot.core;
using bot.kraken.Model;
using bot.model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace bot.kraken.test
{
    public class KrackenClientTests
    {
        [Test]
        public async Task GetServerTime()
        {
            var cr = new KrakenClientService(new Config());
            var time = await cr.GetServerTime();
            Assert.That(time.Rfc1123, Is.Not.Null);
        }

        [Test]
        public async Task GetAssetInfo()
        {
            var cr = new KrakenClientService(new Config());
            var result = await cr.GetAssetInfo();
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetEtherAssetInfo()
        {
            var cr = new KrakenClientService(new Config());
            var result = await cr.GetAssetInfo(assets: "ETH");
            Assert.That(result.Values.First().Altname, Is.EqualTo("ETH"));
        }

        [Test]
        public async Task GetAssetPairs()
        {
            var cr = new KrakenClientService(new Config());
            var result = await cr.GetTradableAssetPairs();
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetTrades()
        {
            var cr = new KrakenClientService(new Config());
            var result = await cr.GetTrades(pairs: "ETHUSD");
            Assert.That(result.LastId, Is.Not.Null);
            Assert.That(result.Results, Is.Not.Null);
        }

        [Test]
        public void BuildUrl()
        {
            var cr = new KrakenClientService(new Config());
            var url = cr.BuildPublicPath("Asset", new Dictionary<string, string>() { { "asset", "ETH" } });

            Assert.That(url, Is.EqualTo("https://api.kraken.com/0/public/Asset?asset=ETH"));

        }

        [Test]
        public async Task GetBalance()
        {
            var cr = new KrakenClientService(await GetConfig());
            var result = await cr.CallPrivate<Dictionary<string, decimal>>("Balance");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetClosedOrders()
        {
            var cr = new KrakenClientService(await GetConfig());
            var orders = await cr.GetClosedOrders();
            Assert.That(orders.Count, Is.GreaterThan(1));
        }

        private async Task<Config> GetConfig()
        {
            var db = new ConfigRepository();
            return await db.Get();
        }

        private async Task<KrakenClientService> Client()
        {
            var config = await GetConfig();
            return new KrakenClientService(config);
        }

        [Test]
        public async Task GetTradeBalanceForAsset()
        {
            //var cr = new KrakenClient(await GetConfig());
            var tradebalance = await (await Client()).CallPrivate<string>("TradeBalance", new Dictionary<string, string>() { { "asset", "ETH" } });
            Assert.That(tradebalance, Is.Not.Null);
        }


        [Test]
        public void StrToEnumTest()
        {
            Assert.That("sell".ToEnum<OrderType>(), Is.EqualTo(OrderType.sell));
        }

        [Test]
        public void Volume()
        {
            Console.WriteLine(0.0002m.ToString());
        }

        [Test]
        [Ignore("It is actually buying")]
        public async Task Buy()
        {
            await (await Client()).AddOrder(OrderType.buy, 0.04m);
        }

        [Ignore("It is actually buying")]
        [Test]
        public async Task Sell()
        {
            var result =await (await Client()).AddOrder(OrderType.sell, 0.08m);
        }




        [Test]
        public void Desearialize_AddOrder()
        {
            var response = JsonConvert.DeserializeObject<Dictionary<string, object>>("{\"descr\":{\"order\":\"buy 0.04000000 ETHUSD @ market\"},\"txid\":[\"ODZDYQ-FEQJZ-EHJRYP\"]}");
            var ids = response["txid"];
            var id2 = JsonConvert.DeserializeObject<List<string>>(ids.ToString());
            Assert.That(id2, Is.Not.Null);
        }

        [Test]
        public async  Task GetOrderInfo()
        {
            var result = await (await Client()).GetOrdersInfo("O5YA6T-IXSJS-3GPWKN");

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetOrder()
        {
            var result = await (await Client()).GetOrders("ON3ZLP-TJK5D-B7UN23", "OWFXTE-VNVDA-6LLX6X");

            Assert.That(result, Is.Not.Null);
        }
    }
}
