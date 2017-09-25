using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace bot.kraken.app
{
    class Program
    {
        static void Main(string[] args)
        {
            var pair = "ETHUSD";
            var client = new KrakenClient();

            var db = new DatabaseService();
            while (true)
            {

                Task.Run(async () =>
                {
                    var id = await db.GetId(pair);

                    var getTradesReasult = await client.GetTrades(id, pair);
                    Console.WriteLine($"Loaded {getTradesReasult.Results.Count} trades at {DateTime.Now}. Last id: {getTradesReasult.LastId}");

                    await db.SaveLastId(pair, getTradesReasult.LastId);
                    await db.Save(getTradesReasult.Results);

                });
                Thread.Sleep(10 * 60 * 1000);

            }
        }
    }
}
