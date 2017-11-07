using System.Threading.Tasks;
using bot.core.Extensions;
using bot.kraken;
using bot.model;
using Microsoft.Practices.Unity;
using NUnit.Framework;

namespace bot.core.tests
{
    public class OrderServiceIntegrationTests
    {
        private const string Pair = "ETHUSD";

        private UnityContainer _container;
        private IOrderService _orderService;
        private IExchangeClient _exchangeClient;

        [SetUp]
        public async Task Setup()
        {
            _container = new UnityContainer();
            _container.RegisterAssembleyWith<ILogRepository>();
            _container.RegisterType<IExchangeClient, KrakenClientService>("kraken");
            
            var configRepo = _container.Resolve<IConfigRepository>();
            _container.RegisterInstance(await configRepo.Get());
            var config = _container.Resolve<Config>();
            config.PairPercent[Pair] = 10;

            _exchangeClient = _container.Resolve<IExchangeClient>("kraken");
            _orderService = _container.Resolve<IOrderService>();
        }

        [Test]
        public async Task Buy()
        {
            await _orderService.Buy(_exchangeClient, Pair, 296.09m);
        }

        [Test]
        public async Task Check()
        {
            await _orderService.CheckOpenOrders(_exchangeClient);
        }

        [Test]
        public async Task Sell()
        {
            await _orderService.Sell(_exchangeClient, Pair, 297.80m);
        }
    }
}
