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
               var loaderService =  _container.Resolve<ILoaderService>();
                loaderService.Load().ContinueWith(r =>
                {
                    _container.RegisterInstance(r.Result);
                    _timer.Interval = _container.Resolve<Config>().LoadIntervalMinutes * 60 * 1000;
                }).Wait(new TimeSpan(0,0,2));
                
            }
            finally
            {
                _runCompleteEvent.Set();
            }
        }
    }
}
