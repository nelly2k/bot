using System;
using System.ServiceProcess;
using System.Threading;
using bot.core;
using bot.core.Extensions;
using bot.kraken;
using bot.model;
using Microsoft.Practices.Unity;
using Timer = System.Timers.Timer;

namespace bot.service.loader
{
    public partial class Loader : ServiceBase
    {
        private Timer _timer;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent _runCompleteEvent = new ManualResetEvent(false);
        private UnityContainer _container;
        private IFileService _fileService;

        public Loader()
        {
            InitializeComponent();
            ConfigContainer();
        }

        private void ConfigContainer()
        {
            _container = new UnityContainer();
            _container.RegisterInstance(new Config());
            _container.RegisterAssembleyWith<ITradeRepository>();
            _container.RegisterType<IExchangeClient, KrakenClientService>("kraken");
            _container.RegisterDateTime();
            _container.RegisterInstance<IRandom>(new MyRandom());
            _fileService = _container.Resolve<IFileService>();
        }

        protected override void OnStart(string[] args)
        {
            _timer = new Timer(_container.Resolve<Config>().LoadIntervalMinutes * 60 * 1000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
            Timer_Elapsed(null, null);
        }

        protected override void OnStop()
        {
            _cancellationTokenSource.Cancel();
            _runCompleteEvent.WaitOne();
            base.OnStop();
        }
     
        void Timer_Elapsed(object sender, EventArgs e)
        {
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
                    _fileService.Write("loader", "Config wasn't loaded");
                    return;
                }
                _container.RegisterInstance(config);

                var loaderService = _container.Resolve<ILoaderService>();
                loaderService.Load().Wait(new TimeSpan(0, 0, 2));
                _timer.Interval = config.LoadIntervalMinutes * 60 * 1000;
            }
            catch (Exception ex)
            {
                _fileService.Write("loader", ex.Message);
                _fileService.Write("loader", ex.StackTrace);
            }
            finally
            {
                _runCompleteEvent.Set();
            }
        }
    }
}
