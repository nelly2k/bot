using System;
using System.Threading;
using System.Threading.Tasks;
using bot.model;

namespace bot.core.trading
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start trading");
            var service = new CoreService();
            var db = new DatabaseService();
            Config config = null;
            while (true)
            {
                if (!ConnectivityService.CheckForInternetConnection())
                {
                    Console.WriteLine($"{DateTime.Now:F} Connection is not available");
                    Thread.Sleep(10 * 60 * 1000);
                    continue;
                }
                
                try
                {
                    var mainTask = Task.Run(async () =>
                    {
                        Console.WriteLine($"{DateTime.Now:F} Start Analysis");
                        config = await db.GetConfig();
                        await service.SetStatus(config);
                    });
                    Task.WaitAll(new [] {mainTask}, new TimeSpan(2* 60 * 1000));
                    Console.WriteLine($"{DateTime.Now:F} Status: { CoreService.TradeStatus.ToString()}");
                   
                    Thread.Sleep((config?.LoadIntervalMinutes??5) * 60 * 1000);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
