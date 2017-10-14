using System.Linq;
using System.Threading.Tasks;
using bot.core.Extensions;
using bot.kraken;
using bot.model;
using Microsoft.Practices.Unity;
using NSubstitute;
using NUnit.Framework;

namespace bot.core.tests
{
    public class IntegrationTests
    {
        private UnityContainer _container;
        private IEventRepository _eventRepository;

        [SetUp]
        public void Setup()
        {
            _container = new UnityContainer();
            _container.RegisterInstance(new Config());
            _container.RegisterAssembleyWith<IDatabaseService>();
            _container.RegisterType<IExchangeClient, KrakenClientService>("kraken");
            _container.RegisterDateTime();

            _container.RegisterInstance<IDatabaseService>(new DatabaseService());
            _eventRepository = NSubstitute.Substitute.For<IEventRepository>();
        }

        [Test]
        public async Task LoadTradesToDb()
        {
            var loaderService = _container.Resolve<ILoaderService>();
            await loaderService.Load();
        }


        [Test]
        public void StatusUpdatesIntersepted()
        {
            var currentStatus = TradeStatus.Unknown;

            currentStatus = CatchStatsuChanges(currentStatus);
            _eventRepository.UpdateLastEvent("", "", "Sell");
            Assert.That(currentStatus,Is.EqualTo(TradeStatus.Sell));
            _eventRepository.UpdateLastEvent("", "", "Buy");
            Assert.That(currentStatus, Is.EqualTo(TradeStatus.Buy));
        }

        [Test]
        public void Trade()
        {
            var currentStatus = TradeStatus.Unknown;
            currentStatus = CatchStatsuChanges(currentStatus);


        }

        private TradeStatus CatchStatsuChanges(TradeStatus currentStatus)
        {
            _eventRepository.When(x => x.UpdateLastEvent(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()))
                .Do(x =>
                {
                    var newStatus = x.Args().Last().ToString().ToEnum<TradeStatus>();
                    if (currentStatus != newStatus && newStatus != TradeStatus.Unknown)
                    {
                        currentStatus = newStatus;
                    }
                });
            return currentStatus;
        }
    }
}
