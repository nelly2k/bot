using System;
using System.Threading;
using System.Threading.Tasks;
using bot.model;
using Timer = System.Timers.Timer;

namespace bot.core.trading
{
    class Program
    {
        private static TradeService _tradeService;
        private static DatabaseService _db;
        private static Timer _timer;
        private static ConfigRepository _configRepository;

        static void Main(string[] args)
        {
          
            _db = new DatabaseService();
            _configRepository = new ConfigRepository();
            _timer = new Timer(3 * 60 * 1000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Start();
            
            Console.ReadLine();
        }

        static void Timer_Elapsed(object sender, EventArgs e)
        {
           
            Task.Run(RunAsync).ContinueWith(r =>
            {
                _timer.Interval = (r.Result?.LoadIntervalMinutes ?? 3) * 60 * 1000;
                if (r.IsFaulted && r.Exception != null)
                {
                    
                    Console.WriteLine("An exception here");
                    Exception ex = r.Exception;

                    while (ex is AggregateException && ex.InnerException != null)
                    {
                        ex = ex.InnerException;
                    }
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }).Wait(new TimeSpan((long) (1.5 * 60 * 1000)));
        }

        static async Task<Config> RunAsync()
        {
            var config = await _configRepository.Get();
            await _tradeService.SetStatus(config).ContinueWith(r =>
            {
                if (r.IsFaulted && r.Exception != null)
                {
                    Console.WriteLine("An exception here");
                    Exception ex = r.Exception;

                    while (ex is AggregateException && ex.InnerException != null)
                    {
                        ex = ex.InnerException;
                    }
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            });
            return config;
           
        }
    }
}
