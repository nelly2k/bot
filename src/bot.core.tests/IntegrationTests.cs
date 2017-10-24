using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bot.core.Extensions;
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
        private IExchangeClient _exchangeClient;

        private const string pair = "XETHZUSD";

        [SetUp]
        public void Setup()
        {
            _container = new UnityContainer();
            _container.RegisterType<ITradeRepository, TradeRepository>();
            _container.RegisterType<IOrderService, OrderService>();
            _container.RegisterType<IMoneyService, MoneyService>();
            _container.RegisterType<ITradeService, TradeService>();
            _container.RegisterInstance(new Config());

            _container.RegisterInstance(Substitute.For<IDateTime>());
            _container.RegisterInstance(Substitute.For<IEventRepository>());
            _container.RegisterInstance(Substitute.For<IOrderRepository>());
            _container.RegisterInstance(Substitute.For<IBalanceRepository>());
            _container.RegisterInstance(Substitute.For<ILogRepository>());
            _container.RegisterInstance("kraken", Substitute.For<IExchangeClient>());


            _eventRepository = _container.Resolve<IEventRepository>();

            var clients = _container.ResolveAll<IExchangeClient>();
            _exchangeClient = clients.First();
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

            SetupStatus(currentStatus, newStatus => currentStatus = newStatus);
            _eventRepository.UpdateLastEvent("", "", "Sell");
            Assert.That(currentStatus, Is.EqualTo(TradeStatus.Sell));
            _eventRepository.UpdateLastEvent("", "", "Buy");
            Assert.That(currentStatus, Is.EqualTo(TradeStatus.Buy));
        }

        private void SetupStatus(TradeStatus currentStatus, Action<TradeStatus> setAction)
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

        private void SetCurrentStatus(TradeStatus status)
        {
            _eventRepository.GetLastEventValue(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(status.ToString()));
        }

        private async Task SetConfig()
        {
            var repo = new ConfigRepository();
            var config = await repo.Get();
            _container.RegisterInstance<Config>(config);

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

        private void SetupExchangeService(OrderType type, Action<decimal, decimal> setVolumePrice)
        {
            _exchangeClient.When(x => x.AddOrder(type, Arg.Any<decimal>(), Arg.Any<decimal>()))
                .Do(x =>
                {
                    setVolumePrice?.Invoke(Convert.ToDecimal(x.Args()[1]), Convert.ToDecimal(x.Args()[2]));
                });
        }

        [Test]
        public async Task StatusIsSet()
        {
            var tradeSer = _container.Resolve<ITradeService>();
            SetCurrentStatus(TradeStatus.Buy);
            Assert.That(await tradeSer.GetCurrentStatus(), Is.EqualTo(TradeStatus.Buy));
            SetCurrentStatus(TradeStatus.Sell);
            Assert.That(await tradeSer.GetCurrentStatus(), Is.EqualTo(TradeStatus.Sell));

        }

        [Test]
        public void SetDateWorks()
        {
            SetDate(new DateTime(2015, 05, 10));
            Assert.That(_container.Resolve<IDateTime>().Now, Is.EqualTo(new DateTime(2015, 05, 10)));
        }

        [Test]
        public void SetOrderService_Buy()
        {
            var price = decimal.Zero;
            SetupOrderService(x => price = x, null);
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

        private void SetUsdBalance(decimal balance)
        {
            _exchangeClient.GetBaseCurrencyBalance().Returns(Task.FromResult(balance));
        }

        private void SetEthBalance(decimal volume, decimal price)
        {
            var balanceRepository = _container.Resolve<IBalanceRepository>();
            balanceRepository.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(
                new List<BalanceItem>()
                {
                    new BalanceItem()
                    {
                        Volume = volume,
                        Price = price

                    }
                }));
        }

        [Test]
        public async Task RunSimulator()
        {
            var dateTime = DateTime.Now.AddHours(-10);
            await TradeSimulator(dateTime, DateTime.Now);
        }
        private async Task TradeSimulator(DateTime start, DateTime end)
        {
            var file = Write("simulator", true, "Status,ETH,Price,USD balance");
            await SetConfig();
            var currentStatus = TradeStatus.Unknown;
            var config = _container.Resolve<Config>();
            var tradeService = _container.Resolve<ITradeService>();

            var ethBalance = decimal.Zero;
            var usdBalance = decimal.Zero;
            var price = decimal.Zero;

            SetUsdBalance(65m);
            SetCurrentStatus(currentStatus);

            SetupExchangeService(OrderType.buy, (vol, pr) =>
            {
                ethBalance = ethBalance + vol;
                usdBalance = usdBalance - ((vol * pr) + (vol * pr / 100 * 0.26m));
                SetUsdBalance(usdBalance);
                SetEthBalance(ethBalance, pr);
                Write(file, false, $"buy,{ethBalance},{price},{usdBalance}");
            });

            SetupExchangeService(OrderType.sell, (vol, pr) =>
           {
               ethBalance = ethBalance - vol;
               usdBalance = usdBalance + (vol * pr) - (vol * pr / 100 * 0.16m);
               SetUsdBalance(usdBalance);
               SetEthBalance(ethBalance, pr);
               Write(file, false, $"sell,{ethBalance},{price},{usdBalance}");
           });

            SetupStatus(currentStatus, newStatus =>
            {
                currentStatus = newStatus;
                SetCurrentStatus(newStatus);

            });

            var currentTime = start;

            while (currentTime < end)
            {
                SetDate(currentTime);
                await tradeService.Trade();
                currentTime = currentTime.AddMinutes(config.LoadIntervalMinutes);
            }
        }


        public string Write(string name, bool isNewFile, params string[] lines)
        {
            var filename = isNewFile ? $"h:\\{name}_{DateTime.Now:yyMMddhhmmss}.csv" : name;
            using (var file = new System.IO.StreamWriter(filename, true))
            {
                foreach (var line in lines)
                {
                    file.WriteLine(line);
                }
                file.Close();
            }
            return filename;
        }
    }
}
