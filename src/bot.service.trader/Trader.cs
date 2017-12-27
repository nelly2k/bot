using System;
using System.ServiceProcess;
using System.Threading;
using bot.core;
using bot.core.Extensions;
using bot.kraken;
using bot.model;
using Microsoft.Practices.Unity;
using Timer = System.Timers.Timer;

namespace bot.service.trader
{
    public partial class Trader : ServiceBase
    {
        private Timer _timer;
        private readonly ManualResetEvent _runCompleteEvent = new ManualResetEvent(false);
        private UnityContainer _container;
        private IFileService _fileService;

        public Trader()
        {
            InitializeComponent();
            ConfigContainer();
        }

        private void ConfigContainer()
        {
            _container = new UnityContainer();
            _container.RegisterInstance(new Config());
            _container.RegisterAssembleyWith<ITradeRepository>();
            _container.RegisterAssembleyWith<IKrakenRepository>();
            _container.RegisterType<IExchangeClient, KrakenClientService>("kraken");
            _container.RegisterType<IExchangeConfig, KrakenConfig>("kraken");
            _container.RegisterDateTime();
            _container.RegisterInstance<IRandom>(new MyRandom());
            _container.RegisterType<IFileService, FileService>(new ContainerControlledLifetimeManager());
            _fileService = _container.Resolve<IFileService>();
        }

        protected override void OnStart(string[] args)
        {
            _timer = new Timer(_container.Resolve<Config>().LoadIntervalMinutes * 60 * 1000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
            Timer_Elapsed(null, null);
        }

        void Timer_Elapsed(object sender, EventArgs e)
        {
            try
            {
                _timer.Stop();
                var configRepository = _container.Resolve<IConfigRepository>();
                Config config = null;
                configRepository.Get("kraken").ContinueWith(configResponse =>
                {
                    config = configResponse.Result;

                }).Wait(); 
                if (config == null)
                {
                    _fileService.Write("error","Config wasn't loaded");
                    return;
                }
                _container.RegisterInstance(config);

                var tradeService = _container.Resolve<ITradeService>();
                tradeService.Trade().ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        _fileService.Write("error", task.Exception);
                    }
                    _timer.Start();
                }).Wait();

                _timer.Interval = config.LoadIntervalMinutes * 60 * 1000;

            }
            catch (Exception ex)
            {
                _fileService.Write("error", ex);
            }
            finally
            {
                _runCompleteEvent.Set();
                _timer.Start();
            }
        }
    }
}
