using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
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
        private const int DefaultInterval = 3;
        private int _interval = DefaultInterval;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent _runCompleteEvent = new ManualResetEvent(false);
        private UnityContainer _container;
        private IExchangeClient _client;

        public Loader()
        {
            InitializeComponent();
            ConfigContainer();

            _client = _container.Resolve<IExchangeClient>();
        }

        private void ConfigContainer()
        {
            _container = new UnityContainer();
            _container.RegisterAssembleyWith<IKrakenDataService>();
            _container.RegisterAssembleyWith<IFileService>();
            _container.RegisterInstance<Config>(new Config());

            _container.RegisterType<IExchangeClient, KrakenClientService>();

        }

        protected override void OnStart(string[] args)
        {
            _timer = new Timer(_interval * 60 * 1000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        protected override void OnStop()
        {
            _cancellationTokenSource.Cancel();
            _runCompleteEvent.WaitOne();
            base.OnStop();
        }

        async Task RunAsync(CancellationToken cancellationToken)
        {
            _timer.Stop();
            while (!cancellationToken.IsCancellationRequested)
            {
                const string pair = "ETHUSD";
             
                var db = new KrakenDataService();
                var coreDb = new DatabaseService();

                if (!ConnectivityService.CheckForInternetConnection())
                {
                    _interval = 10;
                    _timer.Start();
                    return;
                }

                var id = await db.GetId(pair);

                var getTradesReasult = await _client.GetTrades(id, pair);
                if (!string.IsNullOrEmpty(getTradesReasult.LastId))
                {
                    await db.SaveLastId(pair, getTradesReasult.LastId);
                }
                await db.Save(getTradesReasult.Results);
                await coreDb.UpdateLastEvent("kraken load");
                var config = await coreDb.GetConfig();
                _container.RegisterInstance<Config>(config);

                _timer.Interval = (config?.LoadIntervalMinutes ?? DefaultInterval) * 60 * 1000;
            }
            _timer.Start();
        }

        void Timer_Elapsed(object sender, EventArgs e)
        {
            try
            {
                RunAsync(_cancellationTokenSource.Token).ContinueWith(r =>
                {
                    if (r.IsFaulted && r.Exception != null)
                    {
                        Exception ex = r.Exception;
                        while (ex is AggregateException && r.Exception.InnerException != null)
                        {
                            ex = ex.InnerException;
                        }
                        //TODO write
                        //Write($"{DateTime.Now:F}");
                        //Write(ex.Message);
                        //Write(ex.StackTrace);
                    }
                }).Wait(new TimeSpan(60 * 1000));
            }
            finally
            {
                _runCompleteEvent.Set();
            }

        }


    }
}
