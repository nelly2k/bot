using System;
using System.Threading;
using System.Threading.Tasks;
using bot.core;
using bot.model;

namespace bot.kraken.app
{
    class Program
    {
        static void Main(string[] args)
        {
            var pair = "ETHUSD";
            var client = new KrakenClient();

            var db = new KrakenDatabaseService();
            var coreDb = new DatabaseService();
            Config config=null;
            
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
                        config = await coreDb.GetConfig();

                        var id = await db.GetId(pair);

                        var getTradesReasult = await client.GetTrades(id, pair);
                        await db.SaveLastId(pair, getTradesReasult.LastId);
                        await db.Save(getTradesReasult.Results);

                        Console.WriteLine($"{DateTime.Now:F} Loaded {getTradesReasult.Results.Count} trades");

                    });
                    Task.WaitAll(new[] { mainTask },  new TimeSpan(2* 60 * 1000));
                    Thread.Sleep((config?.LoadIntervalMinutes??5) * 60 * 1000);
                }
                catch (Exception e)
                {
                    Task.WaitAll(Task.Run(async () =>
                    {
                        await coreDb.Log("kraken", "loadingError", e.Message);
                    }));
                }
            }
        }
    }
}
