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
            container.RegisterAssembleyWith< IKrakenDataService>();
            container.RegisterInstance<IApiCredentials>(new Config());
            var client = container.Resolve<IKrakenClientService>();

            Assert.That(client, Is.Not.Null);
        }
    }
}