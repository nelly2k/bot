using System;
using System.Linq;
using System.Threading.Tasks;
using bot.core.Extensions;
using bot.model;

namespace bot.core
{
    public class CoreService
    {

        public static TradeStatus TradeStatus = TradeStatus.Unknown;

        public async Task SetStatus(Config config)
        {
            try
            {
                var databaseService = new DatabaseService();
                var dt = DateTime.Now.AddHours(-config.AnalyseLoadHours);
                var trades = await databaseService.LoadTrades("XETHZUSD", dt);

                var grouped = trades.GroupAll(config.AnalyseGroupPeriodMinutes, GroupBy.Minute).ToList();
                var macd = grouped.Macd(config.AnalyseMacdSlow, config.AnalyseMacdFast, config.AnalyseMacdSignal).MacdAnalysis();
                var rsiLastPeak = grouped.RelativeStrengthIndex(config.AnalyseRsiEmaPeriods).GetPeaks(config.AnalyseRsiLow, config.AnalyseRsiHigh).OrderByDescending(x => x.PeakTrade.DateTime)
                    .FirstOrDefault();
                var newStatus = AnalysisExtensions.AnalyseIndeces(config.AnalyseTresholdMinutes, DateTime.Now, macd, rsiLastPeak);

                Console.WriteLine($"{DateTime.Now:F} Price: {grouped.Last().Price:C} Status: {newStatus.ToString()}");
                if (newStatus != TradeStatus && newStatus != TradeStatus.Unknown)
                {
                    var price = grouped.First(x => x.DateTime == macd.Trade.DateTime);
                    TradeStatus = newStatus;
                    System.Media.SystemSounds.Asterisk.Play();
                    await databaseService.Log("kraken", TradeStatus.ToString(),
                        $"New status: {newStatus} price: {price.Price:C}");
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
