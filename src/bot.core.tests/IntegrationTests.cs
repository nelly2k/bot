using System;
using System.Collections.Generic;
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
        private IKrakenClientService _krakenClientService;

        private const string pair = "XETHZUSD";

        [SetUp]
        public void Setup()
        {
            _container = new UnityContainer();
            _container.RegisterInstance(new Config());
            _container.RegisterAssembleyWith<IDatabaseService>();
            _krakenClientService = Substitute.For<IKrakenClientService>();
            _container.RegisterInstance<IExchangeClient>("kraken", _krakenClientService);
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

            CatchStatusChanges(currentStatus, newStatus => currentStatus = newStatus);
            _eventRepository.UpdateLastEvent("", "", "Sell");
            Assert.That(currentStatus,Is.EqualTo(TradeStatus.Sell));
            _eventRepository.UpdateLastEvent("", "", "Buy");
            Assert.That(currentStatus, Is.EqualTo(TradeStatus.Buy));
        }

        [Test]
        public void Trade()
        {
            
            var currentStatus = TradeStatus.Unknown;
            CatchStatusChanges(currentStatus,newStatus=> currentStatus = newStatus);

            var currentBalance = 65m;

            _krakenClientService.GetBalance().Returns(new Dictionary<string, decimal>{{pair, currentBalance}});

            var config = new Config();
            config.PairPercent.Add("XETHZUSD",60);
            var mypercent = config.PairPercent["XETHZUSD"];

            var moneyToSpend = currentBalance / 100m * (decimal)mypercent;


        }

        private void CatchStatusChanges(TradeStatus currentStatus, Action<TradeStatus> setAction)
        {
            _eventRepository.When(x => x.UpdateLastEvent(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()))
                .Do(x =>
                {
                    var newStatus = x.Args().Last().ToString().ToEnum<TradeStatus>();
                    if (currentStatus != newStatus && newStatus != TradeStatus.Unknown)
                    {
                        setAction(newStatus);
                    }
                });
        }

        [Test]
        public async Task FindCurrentBuyPrice()
        {
            var db = new DatabaseService();
            var dt = DateTime.Now.AddMinutes(-30);
            var trades = await db.LoadTrades("XETHZUSD", dt);
            var grouped = trades.GroupAll(10, GroupBy.Minute);
        }
    }
}
