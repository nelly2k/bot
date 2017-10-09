using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using bot.core;
using bot.kraken;
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

        public Loader()
        {
            InitializeComponent();
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
                var client = new KrakenClient();

                var db = new KrakenDatabaseService();
                var coreDb = new DatabaseService();

                if (!ConnectivityService.CheckForInternetConnection())
                {
                    _interval = 10;
                    _timer.Start();
                    return;
                }

                var id = await db.GetId(pair);

                var getTradesReasult = await client.GetTrades(id, pair);
                if (!string.IsNullOrEmpty(getTradesReasult.LastId))
                {
                    await db.SaveLastId(pair, getTradesReasult.LastId);
                }
                await db.Save(getTradesReasult.Results);
                await coreDb.UpdateLastEvent("kraken load");

                var config = await coreDb.GetConfig(); ;
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
                        Write($"{DateTime.Now:F}");
                        Write(ex.Message);
                        Write(ex.StackTrace);
                    }
                }).Wait(new TimeSpan(60 * 1000));
            }
            finally
            {
                _runCompleteEvent.Set();
            }

        }

        public void Write(string message)
        {
            using (var file = new System.IO.StreamWriter($"h:\\loader_log.txt", true))
            {
                file.WriteLine(message);
                file.Close();
            }
        }
    }
}
