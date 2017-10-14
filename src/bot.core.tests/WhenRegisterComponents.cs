using System.Linq;
using bot.core.Extensions;
using bot.kraken;
using bot.model;
using Microsoft.Practices.Unity;
using NUnit.Framework;

namespace bot.core.tests
{
    public class WhenRegisterComponents
    {
        [Test]
        public void RegisterAndResolve()
        {
            var container = new UnityContainer();
            container.RegisterType<IExchangeClient, KrakenClientService>("kraken");
            container.RegisterInstance<IApiCredentials>(new Config());
            var client = container.ResolveAll<IExchangeClient>();

            Assert.That(client.Count(), Is.GreaterThan(0));
        }
    }
}