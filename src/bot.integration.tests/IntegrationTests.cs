using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bot.core;
using bot.model;
using Microsoft.Practices.Unity;
using NSubstitute;
using NUnit.Framework;

namespace bot.integration.tests
{
    public class IntegrationTests
    {
        private UnityContainer _container;
        private IExchangeClient _exchangeClient;
        private IFileService _fileService;

        [SetUp]
        public void Setup()
        {
            _container = new UnityContainer();

            _container.RegisterType<IOrderService, OrderService>();
            _container.RegisterType<IMoneyService, MoneyService>();
            _container.RegisterType<ITradeService, TradeService>();
            _container.RegisterType<IFileService, FileService>(new ContainerControlledLifetimeManager());
            _container.RegisterType<IConfigRepository, ConfigRepository>();
            _container.RegisterInstance(new Config());

            _container.RegisterInstance(Substitute.For<IDateTime>());
            _container.RegisterInstance(Substitute.For<IEventRepository>());
            _container.RegisterInstance(Substitute.For<IOrderRepository>());
            _container.RegisterInstance(Substitute.For<IBalanceRepository>());
            _container.RegisterInstance(Substitute.For<ILogRepository>());
            _container.RegisterInstance(Substitute.For<ITradeRepository>());
            _container.RegisterInstance(Substitute.For<INotSoldRepository>());
            _container.RegisterInstance(Substitute.For<IStatusService>());
            _container.RegisterInstance(Substitute.For<IOperationRepository>());
            _container.RegisterInstance("kraken", Substitute.For<IExchangeClient>());
            
           
        }

        [Test]
        public async Task LoadTradesToDb()
        {
            var loaderService = _container.Resolve<ILoaderService>();
            await loaderService.Load();
        }

        private void SetCurrentStatus(TradeStatus status)
        {
            _statusService.GetCurrentStatus(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(status));
        }

        private void SetDate(DateTime dt)
        {
            var ser = _container.Resolve<IDateTime>();
            ser.Now.Returns(dt);
        }

        private void SetBorrow(Action<decimal, decimal> setVolumePrice)
        {
            _exchangeClient.When(x => x.Sell(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<int?>(), true))
                .Do(x =>
                {
                    setVolumePrice?.Invoke(Convert.ToDecimal((object)x.Args()[0]), Convert.ToDecimal((object)x.Args()[2]));
                });


            _exchangeClient.Sell(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<int?>(), true)
                .ReturnsForAnyArgs(new List<string> { "order" });
        }

        private void SetReturn(Action<decimal, decimal> setVolumePrice)
        {
            _exchangeClient.When(x => x.Buy(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<int?>(), true))
                .Do(x =>
                {
                    setVolumePrice?.Invoke(Convert.ToDecimal((object)x.Args()[0]), Convert.ToDecimal((object)x.Args()[2]));
                });


            _exchangeClient.Buy(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<int?>(), true)
                .ReturnsForAnyArgs(new List<string> { "order" });
        }

        private void SetSell(Action<decimal, decimal> setVolumePrice)
        {
            _exchangeClient.When(x => x.Sell(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<int?>()))
                .Do(x =>
                {
                    setVolumePrice?.Invoke(Convert.ToDecimal((object)x.Args()[0]), Convert.ToDecimal((object)x.Args()[2]));
                });


            _exchangeClient.Sell(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<int?>())
                .ReturnsForAnyArgs(new List<string> { "order" });
        }

        private void SetBuy(Action<decimal, decimal> setVolumePrice)
        {
            _exchangeClient.When(x => x.Buy(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<int?>()))
                .Do(x =>
                {
                   setVolumePrice?.Invoke(Convert.ToDecimal((object)x.Args()[0]), Convert.ToDecimal((object)x.Args()[2]));
                });
        }

        [Test]
        public async Task StatusIsSet()
        {
            var tradeSer = _container.Resolve<IStatusService>();
            SetCurrentStatus(TradeStatus.Buy);
            Assert.That(await tradeSer.GetCurrentStatus("", ""), Is.EqualTo(TradeStatus.Buy));
            SetCurrentStatus(TradeStatus.Sell);
            Assert.That(await tradeSer.GetCurrentStatus("", ""), Is.EqualTo(TradeStatus.Sell));

        }

        [Test]
        public void SetDateWorks()
        {
            SetDate(new DateTime(2015, 05, 10));
            Assert.That(_container.Resolve<IDateTime>().Now, Is.EqualTo(new DateTime(2015, 05, 10)));
        }


        private void SetUsdBalance(decimal balance, decimal borrow)
        {
            _exchangeClient.GetBaseCurrencyBalance().Returns(Task.FromResult(balance));
            _exchangeClient.GetAvailableMargin(Arg.Any<string>()).Returns(Task.FromResult(borrow));
        }
        
        private void SetupNotSold(Action<BalanceItem> action)
        {
            var nosSoldRespo = _container.Resolve<INotSoldRepository>();
            var balanceRepository = _container.Resolve<IBalanceRepository>();
            nosSoldRespo.When(x => x.SetNotSold(Arg.Any<string>(), Arg.Any<string>()))
                .Do(async d => { action(Enumerable.First<BalanceItem>((await balanceRepository.Get("", "")))); });
        }

        private void SetEthBalance(decimal volume, decimal price, int notSold, bool isBorrowed = false)
        {
            var balanceRepository = _container.Resolve<IBalanceRepository>();
            var result = new List<BalanceItem>();

            if (volume != decimal.Zero)
            {
                result.Add(new BalanceItem
                {
                    Volume = volume,
                    Price = price,
                    NotSold = notSold,
                    IsBorrowed = isBorrowed
                });
            }

            balanceRepository.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(result));
        }

        private List<BaseTrade> trades { get; set; }

        private async Task<decimal> SetupTradeService(string pair, DateTime start, DateTime end)
        {
            var real = new TradeRepository();
            var config = _container.Resolve<Config>();
            trades = (await real.LoadTrades(pair, start.AddHours(-config.Pairs[pair].LoadHours), end)).ToList();

            var repo = _container.Resolve<ITradeRepository>();
            repo.LoadTrades(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(args =>
                {
                    var startDate = Convert.ToDateTime((object)args[1]).AddHours(-config.Pairs[pair].LoadHours);
                    var endDate = Convert.ToDateTime((object)args[2]);
                    return trades.Where(x => x.DateTime > startDate && x.DateTime < endDate);
                });


            return trades.Last().Price;
        }

       

        private void SetOperations()
        {
            var repo = _container.Resolve<IOperationRepository>();

            repo.GetIncomplete(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(new List<OperationItem>()));

        }
        [Test]
        public async Task RunSimulator()
        {
            var dateTime = DateTime.Now.AddHours(-50);
            await SetConfig();
            _exchangeClient = _container.Resolve<IExchangeClient>("kraken");
            _fileService = _container.Resolve<IFileService>();
            _statusService = _container.Resolve<IStatusService>();

            await TradeSimulator(dateTime, DateTime.Now);
        }

        //XBTUSD
        //ETHUSD
        private string pairName = "ETHUSD";
        private IStatusService _statusService;

        private async Task SetConfig()
        {
            var repo = _container.Resolve<IConfigRepository>();
            var config = await repo.Get();
            config.LogPrefix = "simulator" + DateTime.Now.ToString("dhms");
            config.Pairs.Remove("XBTUSD");

            var pair = config.Pairs[pairName];
            pair.IsMarket = false;
            pair.GroupForLongMacdMinutes = 60;
            pair.GroupMinutes = 3;
            pair.ThresholdMinutes = 30;
            pair.LoadHours =30;
            pair.ShouldTrade = true;
            _container.RegisterInstance<Config>(config);
        }


        private async Task TradeSimulator(DateTime start, DateTime end)
        {
            var currentStatus = TradeStatus.Unknown;
            var config = _container.Resolve<Config>();
            var tradeService = _container.Resolve<ITradeService>();
            var notSold = 0;
            var ethBalance = decimal.Zero;
            var usdBalance = 240m;
            var lastSellPrice = decimal.Zero;
            var borrowedEth = decimal.Zero;
            var borrowedEthPrice = decimal.Zero;

            var lastPrice = await SetupTradeService(pairName, start, end);

            SetUsdBalance(usdBalance, Math.Round(usdBalance / lastPrice, 2));
            SetEthBalance(0, 0, 0);
            SetCurrentStatus(currentStatus);
            SetOperations();
            
           var currentTime = start;

            SetupNotSold(item =>
            {
                notSold = notSold + 1;
                SetEthBalance(item.Volume, item.Price, notSold);
            });

            SetBuy((vol, pr) =>
            {
                notSold = 0;
                ethBalance = ethBalance + vol;
                usdBalance = usdBalance - Math.Round(vol * pr + Math.Round(vol * pr / 100 * 0.26m, 2), 2);
                SetUsdBalance(usdBalance, Math.Round(usdBalance/pr,2));
                SetEthBalance(ethBalance, pr, 0);

                _fileService.GatherDetails(pairName, FileSessionNames.CoinBalance, ethBalance);
                SetCurrentStatus(TradeStatus.Buy);
            });

            SetSell((vol, pr) =>
            {
                notSold = 0;
                ethBalance = ethBalance - vol;
                usdBalance = usdBalance + Math.Round(vol * pr, 2) - Math.Round(vol * pr / 100 * 0.16m, 2);
                lastSellPrice = pr;
                _fileService.GatherDetails(pairName, FileSessionNames.UsdBalance, usdBalance);

                SetUsdBalance(usdBalance, Math.Round(usdBalance / pr, 2));
                SetEthBalance(ethBalance, pr, 0);
                SetCurrentStatus(TradeStatus.Sell);
            });


            SetBorrow((vol, pr) =>
            {
                usdBalance = usdBalance -  Math.Round(vol * pr / 100 * 0.16m, 2);
                borrowedEth = vol;
                borrowedEthPrice = pr;
                
                SetEthBalance(borrowedEth, pr, 0, true);
                SetUsdBalance(usdBalance, 0);
                SetCurrentStatus(TradeStatus.Sell);
            });

            SetReturn((vol, pr) =>
            {
                var borrowed = borrowedEth * borrowedEthPrice;
                var returned = vol * pr;

                usdBalance = usdBalance + (borrowed - returned) - Math.Round(vol * pr / 100 * 0.26m, 2);
                _fileService.GatherDetails(pairName, FileSessionNames.UsdBalance, usdBalance);

                SetEthBalance(borrowedEth, pr, 0, true);
                SetUsdBalance(usdBalance, Math.Round(usdBalance / pr, 2));
                SetCurrentStatus(TradeStatus.Buy);
            });

            while (currentTime < end)
            {
                SetDate(currentTime);
                await tradeService.Trade();
                currentTime = currentTime.AddMinutes(config.LoadIntervalMinutes);
            }
        }
    }
}
