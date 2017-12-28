using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bot.core;
using bot.model;
using Newtonsoft.Json;
using NUnit.Framework;

namespace bot.kraken.test
{
    public class KrackenClientTests
    {
        KrakenClientService cr;
        private KrakenRepository _krakenRepository;

        [SetUp]
        public void Setup()
        {
            _krakenRepository = new KrakenRepository(new Config());
            cr = new KrakenClientService(new Config(), _krakenRepository, new KrakenConfig());
        }

        [Test]
        public async Task GetServerTime()
        {
            var time = await cr.GetServerTime();
            Assert.That(time.Rfc1123, Is.Not.Null);
        }

        [Test]
        public async Task GetAssetInfo()
        {
            var result = await cr.GetAssetInfo();
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetEtherAssetInfo()
        {
            var result = await cr.GetAssetInfo(assets: "ETH");
            Assert.That(result.Values.First().Altname, Is.EqualTo("ETH"));
        }

        [Test]
        public async Task GetAssetPairs()
        {
            var result = await cr.GetTradableAssetPairs();
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetTrades()
        {
            var result = await cr.GetTrades(pairs: "ETHUSD");
            Assert.That(result.LastId, Is.Not.Null);
            Assert.That(result.Results, Is.Not.Null);
        }

     

        [Test]
        public async Task GetBalance()
        {
            var result = await _krakenRepository.CallPrivate<Dictionary<string, decimal>>("Balance");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetTradeBalance()
        {
            var result = await _krakenRepository.CallPrivate<Dictionary<string, decimal>>("TradeBalance");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetClosedOrders()
        {
            var orders = await cr.GetClosedOrders();
            Assert.That(orders.Count, Is.GreaterThan(1));
        }

        [Test]
        public async Task GetTradeBalanceForAsset()
        {
            var configRepo = new ConfigRepository(new List<IExchangeConfig>().ToArray());
            var config = await configRepo.Get();
            var repo = new KrakenRepository(config);
            var tradebalance = await repo.CallPrivate<Dictionary<string, decimal>>("TradeBalance", new Dictionary<string, string>(){{"asset", "ETH"}});
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
            await cr.AddOrder(OrderType.buy, 0.02m, "ETHUSD");
        }
        
        [Ignore("It is actually buying")]
        [Test]
        public async Task Sell()
        {
            var result =cr.AddOrder(OrderType.sell,0.08m, "ETHUSD");
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
            var result =await cr.GetOrdersInfo("OFPGUC-GCAXO-V6TCFC");

            Assert.That(result, Is.Not.Null);
            Console.WriteLine(result.First().UserRef);

        }

        [Test]
        public async Task GetOrderInfoByUserRef()
        {
            var result = await cr.GetOrdersIds(1579540280);

            Assert.That(result, Is.Not.Null);
            Console.WriteLine(result.First());

        }

        [Test]
        public async Task GetOrder()
        {
            var configRepo = new ConfigRepository(new List<IExchangeConfig>().ToArray());
            var config = await configRepo.Get();
            var repo = new KrakenRepository(config);
            var service = new KrakenClientService(config, repo, new KrakenConfig());
            var result = await service.GetOrders("OHG6X5-JVBOR-JTW7YA");

            Assert.That(result, Is.Not.Null);
        }


        [Test]
        public async Task GetClosedOrdersByUserRef()
        {
            var closed = await cr.GetClosedOrders(52796429);
            
            Assert.That(closed.Any(), Is.True);
        }

        [Test]
        public async Task GetOpenOrdersByUserref()
        {
            var open = await cr.GetOpenOrders(52796429);

            Assert.That(open.Any(), Is.True);
        }

        //1579540280
    }
}
