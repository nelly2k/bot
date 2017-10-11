using System;
using System.Threading.Tasks;
using bot.core.Extensions;
using bot.kraken;
using bot.model;
using Microsoft.Practices.Unity;
using NUnit.Framework;

namespace bot.core.tests
{
    public class IntegrationTests
    {
        private UnityContainer _container;

        [SetUp]
        public void Setup()
        {
            _container = new UnityContainer();
            _container.RegisterInstance(new Config());
            _container.RegisterAssembleyWith<IKrakenDataService>();
            _container.RegisterAssembleyWith<IDatabaseService>();
            _container.RegisterType<IExchangeClient, KrakenClientService>("kraken");
            _container.RegisterDateTime();
        }

        [Test]
        public async Task LoadTradesToDb()
        {
            var loaderService = _container.Resolve<ILoaderService>();
            await loaderService.Load();
        }

    }
}
