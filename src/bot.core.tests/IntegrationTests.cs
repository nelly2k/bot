using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        [SetUp]
        public void Setup()
        {
            _container = new UnityContainer();

            _container.RegisterType<IOrderService, OrderService>();
            _container.RegisterType<IMoneyService, MoneyService>();
            _container.RegisterType<ITradeService, TradeService>();
            _container.RegisterInstance(new Config());

            _container.RegisterInstance(Substitute.For<IDateTime>());
            _container.RegisterInstance(Substitute.For<IEventRepository>());
            _container.RegisterInstance(Substitute.For<IOrderRepository>());
            _container.RegisterInstance(Substitute.For<IBalanceRepository>());
            _container.RegisterInstance(Substitute.For<ILogRepository>());
            _container.RegisterInstance(Substitute.For<ITradeRepository>());
            _container.RegisterInstance(Substitute.For<INotSoldRepository>());
            _container.RegisterInstance(Substitute.For<IFileService>());
            _container.RegisterInstance("kraken", Substitute.For<IExchangeClient>());

            _eventRepository = _container.Resolve<IEventRepository>();

            _exchangeClient = _container.Resolve<IExchangeClient>("kraken");
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

        private void SetDate(DateTime dt)
        {
            var ser = _container.Resolve<IDateTime>();
            ser.Now.Returns(dt);
        }

        private void SetupExchangeService(OrderType type, Action<decimal, decimal> setVolumePrice)
        {
            _exchangeClient.When(x => x.AddOrder(type, Arg.Any<decimal>(), Arg.Any<decimal>()))
                .Do(x =>
                {
                    setVolumePrice?.Invoke(Convert.ToDecimal(x.Args()[1]), Convert.ToDecimal(x.Args()[2]));
                });

            _exchangeClient.AddOrder(Arg.Any<OrderType>(), Arg.Any<decimal>(), Arg.Any<decimal>())
                .ReturnsForAnyArgs(new List<string>());
        }

        [Test]
        public async Task StatusIsSet()
        {
            var tradeSer = _container.Resolve<ITradeService>();
            SetCurrentStatus(TradeStatus.Buy);
            Assert.That(await tradeSer.GetCurrentStatus("",""), Is.EqualTo(TradeStatus.Buy));
            SetCurrentStatus(TradeStatus.Sell);
            Assert.That(await tradeSer.GetCurrentStatus("", ""), Is.EqualTo(TradeStatus.Sell));

        }

        [Test]
        public void SetDateWorks()
        {
            SetDate(new DateTime(2015, 05, 10));
            Assert.That(_container.Resolve<IDateTime>().Now, Is.EqualTo(new DateTime(2015, 05, 10)));
        }


        private void SetUsdBalance(decimal balance)
        {
            _exchangeClient.GetBaseCurrencyBalance().Returns(Task.FromResult(balance));
        }

        private void SetFile()
        {
            var fileName = $"h:\\simulator_log_{DateTime.Now:yyMMddhhmmss}.txt";
            var fileService = _container.Resolve<IFileService>();
            fileService.When(x => x.Write(Arg.Any<string>(),Arg.Any<string>())).Do(d =>
              {
                  using (var file = new System.IO.StreamWriter(fileName, true))
                  {
                      file.WriteLine(d.Args()[1]);
                      file.Close();
                  }

              });
        }

        private void SetupNotSold(Action<BalanceItem> action)
        {
            var nosSoldRespo = _container.Resolve<INotSoldRepository>();
            var balanceRepository = _container.Resolve<IBalanceRepository>();
            nosSoldRespo.When(x => x.SetNotSold(Arg.Any<string>(), Arg.Any<string>()))
                .Do(async d => { action((await balanceRepository.Get("", "")).First()); });
        }

        private void SetEthBalance(decimal volume, decimal price, int notSold)
        {
            var balanceRepository = _container.Resolve<IBalanceRepository>();
            balanceRepository.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(
                new List<BalanceItem>
                {
                    new BalanceItem
                    {
                        Volume = volume,
                        Price = price,
                        NotSold = notSold
                    }
                }));
        }

        private List<BaseTrade> trades { get; set; }

        private async Task SetupTradeService(DateTime start, DateTime end)
        {
            var real = new TradeRepository();
            var config = _container.Resolve<Config>();
            trades = (await real.LoadTrades("XETHZUSD", start.AddHours(-config.AnalyseLoadHours), end)).ToList();

            var repo = _container.Resolve<ITradeRepository>();
            repo.LoadTrades(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(args =>
                {
                    var startDate = Convert.ToDateTime(args[1]).AddHours(-config.AnalyseLoadHours);
                    var endDate = Convert.ToDateTime(args[2]);
                    return trades.Where(x => x.DateTime > startDate && x.DateTime < endDate);
                });
        }

        [Test]
        public async Task RunSimulator()
        {
            var dateTime = DateTime.Now.AddHours(-60);
            await TradeSimulator(dateTime, DateTime.Now);
        }

        private async Task SetConfig()
        {
            var repo = new ConfigRepository();
            var config = await repo.Get();
            config.AnalyseMacdSlowThreshold = 0.00m;
            config.AnalyseGroupPeriodMinutes = 10;
            config.MaxMissedSells = 6;
            _container.RegisterInstance<Config>(config);

        }
        private async Task TradeSimulator(DateTime start, DateTime end)
        {
            
            var file = Write("simulator", true, "Time,Status,ETH,Price,USD balance");
            await SetConfig();
            var currentStatus = TradeStatus.Unknown;
            var config = _container.Resolve<Config>();
            var tradeService = _container.Resolve<ITradeService>();
            var notSold = 0;
            var ethBalance = decimal.Zero;
            var usdBalance = 65m;
            var lastSellPrice = decimal.Zero;
            SetFile();
            var fileService = _container.Resolve<IFileService>();
            fileService.Write("",config.ToString());
            SetUsdBalance(usdBalance);
            SetEthBalance(0, 0, 0);
            SetCurrentStatus(currentStatus);
            await SetupTradeService(start, end);
            var currentTime = start;

            SetupNotSold(item =>
           {
               notSold = notSold + 1;
               SetEthBalance(item.Volume, item.Price, notSold);
               Write(file, false, $"{currentTime:s},not sold,{ethBalance},-,{notSold}");
           });

            SetupExchangeService(OrderType.buy, (vol, pr) =>
            {
                notSold = 0;
                ethBalance = ethBalance + vol;
                usdBalance = usdBalance - Math.Round(vol * pr + Math.Round(vol * pr / 100 * 0.26m, 2), 2);
                SetUsdBalance(usdBalance);
                SetEthBalance(ethBalance, pr, 0);
                Write(file, false, $"{currentTime:s},buy,{ethBalance},{pr},{usdBalance}");
                SetCurrentStatus(TradeStatus.Buy);
            });

            SetupExchangeService(OrderType.sell, (vol, pr) =>
            {
                notSold = 0;
                ethBalance = ethBalance - vol;
                usdBalance = usdBalance + Math.Round(vol * pr, 2) - Math.Round(vol * pr / 100 * 0.16m, 2);
                SetUsdBalance(usdBalance);
                lastSellPrice = pr;
                SetEthBalance(ethBalance, pr, 0);
                Write(file, false, $"{currentTime:s},sell,{ethBalance},{pr},{usdBalance}");
                SetCurrentStatus(TradeStatus.Sell);
            });

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
