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

        private const string pair = "XETHZUSD";

        [SetUp]
        public void Setup()
        {
            _container = new UnityContainer();
            _container.RegisterType<ITradeRepository, TradeRepository>();
            _container.RegisterType<IOrderService, OrderService>();
            _container.RegisterType<IMoneyService, MoneyService>();
            _container.RegisterInstance(new Config());

            _container.RegisterInstance(Substitute.For<IDateTime>());
            _container.RegisterInstance(Substitute.For<IEventRepository>());
            _container.RegisterInstance(Substitute.For<IOrderRepository>());
            _container.RegisterInstance(Substitute.For<IBalanceRepository>());
            _container.RegisterInstance(Substitute.For<ILogRepository>());
            _container.RegisterInstance("kraken", Substitute.For<IExchangeClient>());
            

            _eventRepository = _container.Resolve<IEventRepository>();
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

        private void SetDate(DateTime dt)
        {
            var ser = _container.Resolve<IDateTime>();
            ser.Now.Returns(dt);
        }

        private void SetupOrderService(Action<decimal> setBuyPrice, Action<decimal> setSellPrice)
        {
            var ser = _container.Resolve<IOrderService>();

            ser.When(x => x.Buy(Arg.Any<IExchangeClient>(), Arg.Any<string>(), Arg.Any<decimal>()))
                .Do(x =>
                {
                    setBuyPrice?.Invoke(Convert.ToDecimal(x.Args().Last()));
                });

            ser.When(x => x.Sell(Arg.Any<IExchangeClient>(), Arg.Any<string>(), Arg.Any<decimal>()))
                .Do(x =>
                {
                    setSellPrice?.Invoke(Convert.ToDecimal(x.Args().Last()));
                });
        }

        [Test]
        public void SetDateWorks()
        {
            SetDate(new DateTime(2015,05,10));
            Assert.That(_container.Resolve<IDateTime>().Now, Is.EqualTo(new DateTime(2015, 05, 10)));
        }

        [Test]
        public void SetOrderService_Buy()
        {
            var price = decimal.Zero;
            SetupOrderService(x=>price = x,null);
            var ser = _container.Resolve<IOrderService>();
            ser.Buy(Substitute.For<IExchangeClient>(), "blah", 12.12m);
            Assert.That(price, Is.EqualTo(12.12m).Within(0.001));
        }

        [Test]
        public void SetOrderService_Sell()
        {
            var price = decimal.Zero;
            SetupOrderService(null, x => price = x);
            var ser = _container.Resolve<IOrderService>();
            ser.Sell(Substitute.For<IExchangeClient>(), "blah", 12.12m);
            Assert.That(price, Is.EqualTo(12.12m).Within(0.001));
        }

        [Test]
        public async Task TradeSimulator(DateTime start, DateTime end)
        {
            var currentStatus = TradeStatus.Unknown;
            CatchStatusChanges(currentStatus, newStatus => currentStatus = newStatus);
            var buyPrice = decimal.Zero;
            var sellPrice = decimal.Zero;

            SetupOrderService(x=>sellPrice = x, x=>buyPrice = x);
            
            var current


        }
    }
}
