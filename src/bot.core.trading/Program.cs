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
        private static IFileService _fileService;

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
        
            try
            {
                var configRepository = _container.Resolve<IConfigRepository>();
                Config config = null;
                configRepository.Get("kraken").ContinueWith(configResponse =>
                {
                    config = configResponse.Result;

                }).Wait();
                if (config == null)
                {
                    _fileService.Write($"{DateTime.Now:G} Config wasn't loaded");
                    return;
                }
                _container.RegisterInstance(config);

                var tradeService = _container.Resolve<ITradeService>();
                tradeService.Trade().ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        _fileService.Write($"{DateTime.Now:G} {task.Exception.Message}");
                        _fileService.Write(task.Exception.StackTrace);
                    }
                }).Wait(new TimeSpan(0, 0, 3));

                _timer.Interval = config.LoadIntervalMinutes * 60 * 1000;

            }
            catch (Exception ex)
            {
                _fileService.Write($"{DateTime.Now:G} {ex.Message}");
                _fileService.Write(ex.StackTrace);
            }
        }

        private static void ConfigContainer()
        {
            _container = new UnityContainer();
            _container.RegisterInstance(new Config());
            _container.RegisterAssembleyWith<ITradeRepository>();
            _container.RegisterType<IExchangeClient, KrakenClientService>("kraken");
            _container.RegisterDateTime();
            _fileService = _container.Resolve<IFileService>();
        }
    }
}
