using System;
using System.Threading;
using System.Threading.Tasks;
using bot.core.Extensions;
using bot.kraken;
using bot.model;
using Microsoft.Practices.Unity;
using Timer = System.Timers.Timer;

namespace bot.core.trading
{
    class Program
    {
        private static Timer _timer;
        private static UnityContainer _container;

        static void Main(string[] args)
        {
            
            ConfigContainer();
            
            _timer = new Timer(_container.Resolve<Config>().LoadIntervalMinutes * 60 * 1000);
            Timer_Elapsed(null, null);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();

            Console.ReadKey();
        }

        static void Timer_Elapsed(object sender, EventArgs e)
        {
            Console.WriteLine($"{DateTime.Now:G} Start trading");
            var configRepository = _container.Resolve<IConfigRepository>();
            configRepository.Get("kraken").ContinueWith(configResponse =>
            {
                _container.RegisterInstance(configResponse.Result);

                
                var tradeService = _container.Resolve<ITradeService>();
                tradeService.Trade().Wait(new TimeSpan(0, 0, 3));
                _timer.Interval = configResponse.Result.LoadIntervalMinutes * 60 * 1000;
            }).Wait(new TimeSpan(0, 0,5));

        }

        private static void ConfigContainer()
        {
            _container = new UnityContainer();
            _container.RegisterInstance(new Config());
            _container.RegisterAssembleyWith<ITradeRepository>();
            _container.RegisterType<IExchangeClient, KrakenClientService>("kraken");
            _container.RegisterDateTime();
        }
    }
}
