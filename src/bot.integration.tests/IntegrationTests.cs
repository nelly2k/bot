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
        public async Task RunSimulator()
        {
            var dateTime = DateTime.Now.AddHours(-300);
            await SetConfig();
            _exchangeClient = _container.Resolve<IExchangeClient>("kraken");
            _fileService = _container.Resolve<IFileService>();

            await TradeSimulator(dateTime, DateTime.Now);
        }

        //XBTUSD
        //ETHUSD
        private string pairName = "ETHUSD";
        private string removePair = "XBTUSD";
        
        private async Task SetConfig()
        {
            var repo = _container.Resolve<IConfigRepository>();
            var config = await repo.Get();
            config.LogPrefix = "simulator" + DateTime.Now.ToString("dhms");
            config.Pairs.Remove(removePair);

            var pair = config.Pairs[pairName];
            pair.IsMarket = false;
            pair.GroupForLongMacdMinutes = 30;
            pair.GroupMinutes = 2;
            pair.ThresholdMinutes = 20;
            pair.LoadHours = 15;
            pair.ShouldTrade = true;
            pair.Share = 95;
            _container.RegisterInstance<Config>(config);
        }

        private async Task TradeSimulator(DateTime start, DateTime end)
        {
            var currentStatus = TradeStatus.Unknown;
            var config = _container.Resolve<Config>();
            var tradeService = _container.Resolve<ITradeService>();
            var usdBalance = 240m;

            var lastPrice = await SetupTradeService(pairName, start, end);
            

            var balances = new List<BalanceItem>()
            {
                new BalanceItem()
                {
                    IsBorrowed = false
                },
                new BalanceItem()
                {
                    IsBorrowed = true
                }
            };

            SetUsdBalance(usdBalance, Math.Round(usdBalance / lastPrice, 2));
            SetEthBalance(balances.Where(x => x.Volume > decimal.Zero).ToList());

            SetCurrentStatus(currentStatus);
            SetOperations();
            
           var currentTime = start;

            SetupNotSold(item =>{item.NotSold++;});

            SetBuy((vol, pr) =>
            {
                usdBalance = usdBalance - Math.Round(vol * pr + Math.Round(vol * pr / 100 * 0.26m, 2), 2);
                var balance = balances.First(x => !x.IsBorrowed);

                balance.Volume = balance.Volume + vol;
                balance.Price = pr;
                balance.NotSold = 0;
                SetEthBalance(balances.Where(x=>x.Volume > decimal.Zero).ToList());

                SetUsdBalance(usdBalance);
                SetCurrentStatus(TradeStatus.Buy);
            });

            SetSell((vol, pr) =>
            {
                usdBalance = usdBalance + Math.Round(vol * pr, 2) - Math.Round(vol * pr / 100 * 0.16m, 2);
                _fileService.GatherDetails(pairName, FileSessionNames.UsdBalance, usdBalance);
                
                SetUsdBalance(usdBalance, Math.Round(usdBalance / pr, 2));
                var balance = balances.First(x => !x.IsBorrowed);
                balance.Volume = balance.Volume - vol;
                balance.NotSold = 0;
                SetEthBalance(balances.Where(x => x.Volume > decimal.Zero).ToList());
                
                SetCurrentStatus(TradeStatus.Sell);
            });


            SetBorrow((vol, pr) =>
            {
                usdBalance = usdBalance -  Math.Round(vol * pr / 100 * 0.16m, 2);
              
                SetUsdBalance(usdBalance, decimal.Zero);

                balances.First(x => x.IsBorrowed).Volume = vol;
                balances.First(x => x.IsBorrowed).Price = pr;
                SetEthBalance(balances.Where(x => x.Volume > decimal.Zero).ToList());

                SetCurrentStatus(TradeStatus.Sell);
            });

            SetReturn((vol, pr) =>
            {
                var trans = balances.First(x => x.IsBorrowed);
                var borrowed = trans.Volume * trans.Price;
                var returned = vol * pr;

                usdBalance = usdBalance + (borrowed - returned) - Math.Round(returned / 100 * 0.26m, 2);
                _fileService.GatherDetails(pairName, FileSessionNames.UsdBalance, usdBalance);
                

                balances.First(x => x.IsBorrowed).Volume = 0;
                SetEthBalance(balances.Where(x => x.Volume > decimal.Zero).ToList());

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

        private void SetCurrentStatus(TradeStatus status)
        {
            var ser = _container.Resolve<IStatusService>();
            ser.GetCurrentStatus(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(status));
        }

        private void SetDate(DateTime dt)
        {
            var ser = _container.Resolve<IDateTime>();
            ser.Now.Returns(dt);
        }

        private void SetBorrow(Action<decimal, decimal> setVolumePrice)
        {
            var client = _container.Resolve<IExchangeClient>("kraken");
            client.When(x => x.Sell(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<int?>(), true))
                .Do(x =>
                {
                    setVolumePrice?.Invoke(Convert.ToDecimal((object)x.Args()[0]), Convert.ToDecimal((object)x.Args()[2]));
                });
            //client.Sell(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<int?>(), true)
            //    .ReturnsForAnyArgs(new List<string> { "order" });
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

        private void SetUsdBalance(decimal balance, decimal? borrow = null)
        {
            var client = _container.Resolve<IExchangeClient>("kraken");
            client.GetBaseCurrencyBalance().Returns(Task.FromResult(balance));
            if (borrow.HasValue)
            {
                client.GetAvailableMargin("ETH").Returns(Task.FromResult(borrow.Value));
            }
        }

        private void SetupNotSold(Action<BalanceItem> action)
        {
            var nosSoldRespo = _container.Resolve<INotSoldRepository>();
            var balanceRepository = _container.Resolve<IBalanceRepository>();
            nosSoldRespo.When(x => x.SetNotSold(Arg.Any<string>(), Arg.Any<string>(), false ))
                .Do(async d => { action((await balanceRepository.Get("", "")).First(x=>!x.IsBorrowed)); });

            nosSoldRespo.When(x => x.SetNotSold(Arg.Any<string>(), Arg.Any<string>(), true))
                .Do(async d => { action((await balanceRepository.Get("", "")).First(x => x.IsBorrowed)); });
        }

        private void SetEthBalance(List<BalanceItem> balance)
        {
            var balanceRepository = _container.Resolve<IBalanceRepository>();
            balanceRepository.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(balance));
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
        public async Task BalanceSetting()
        {
            var balances = new List<BalanceItem>()
            {
                new BalanceItem() {Volume = 0}
            };

            SetEthBalance(balances);


            var repo = _container.Resolve<IBalanceRepository>();

            Assert.That((await repo.Get("","")).First().Volume,Is.EqualTo(0));

            balances.First().Volume = 0.2m;
            var repo2 = _container.Resolve<IBalanceRepository>();

            Assert.That((await repo2.Get("", "")).First().Volume, Is.EqualTo(0.2m));

        }
    }
}
