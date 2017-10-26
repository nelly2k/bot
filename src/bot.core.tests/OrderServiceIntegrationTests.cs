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
        private UnityContainer _container;

        [SetUp]
        public void Setup()
        {
            _container = new UnityContainer();
            _container.RegisterAssembleyWith<ILogRepository>();
            _container.RegisterType<IExchangeClient, KrakenClientService>("kraken");
            

        }

        [Test]
        public async Task Buy()
        {
            var configRepo = _container.Resolve<IConfigRepository>();
            _container.RegisterInstance(await configRepo.Get());
            var config = _container.Resolve<Config>();
            config.PairPercent["XETHZUSD"] = 7;


        }
    }
}
