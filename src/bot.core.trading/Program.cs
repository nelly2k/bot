using System;
using System.Threading;
using System.Threading.Tasks;
using bot.model;
using Timer = System.Timers.Timer;

namespace bot.core.trading
{
    class Program
    {
        private static CoreService _coreService;
        private static DatabaseService _db;
        private static Timer _timer;

        static void Main(string[] args)
        {
            _coreService = new CoreService();
            _db = new DatabaseService();

            _timer = new Timer(10 * 1000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
            Console.ReadLine();
        }

        static void Timer_Elapsed(object sender, EventArgs e)
        {
            _timer.Stop();
            Task.Run(RunAsync).ContinueWith(r =>
            {
                
                if (r.IsFaulted && r.Exception != null)
                {
                    Console.WriteLine("An exception here");
                    Exception ex = r.Exception;

                    while (ex is AggregateException && r.Exception.InnerException != null)
                    {
                        ex = ex.InnerException;
                    }
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }).Wait(new TimeSpan(1 * 60 * 1000));
            
            _timer.Start();
        }

        static async Task RunAsync()
        {
            if (!ConnectivityService.CheckForInternetConnection())
            {
                Console.WriteLine($"{DateTime.Now:F} Connection is not available");
                _timer.Interval = 10 * 60 * 1000;
                return;
            }

            try
            {
                var config = await _db.GetConfig();
                await _coreService.SetStatus(config);
                _timer.Interval = (config?.LoadIntervalMinutes ?? 3) * 60 * 1000;


            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
