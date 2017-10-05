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

                if (newStatus != TradeStatus && newStatus != TradeStatus.Unknown)
                {
                    TradeStatus = newStatus;
                    var price = grouped.First(x => x.DateTime == macd.Trade.DateTime);

                    await databaseService.Log("kraken", TradeStatus.ToString(),
                        $"New status: {newStatus} price: {price.Price:C}");

                    if (macd.CrossType == null)
                    {
                        await databaseService.Log("kraken", TradeStatus.ToString(), "RSI: No crosses registered");
                    }
                    else
                    {
                        await databaseService.Log("kraken", TradeStatus.ToString(),
                            $"MACD {macd.CrossType.ToString()} at {macd.Trade.DateTime:t} - {(DateTime.Now - macd.Trade.DateTime).TotalMinutes} minutes ago");
                    }

                    if (rsiLastPeak == null)
                    {
                        await databaseService.Log("kraken", TradeStatus.ToString(), "RSI: No peaks registered");
                    }
                    else
                    {
                        await databaseService.Log("kraken", TradeStatus.ToString(),
                            $"RSI last peak was {rsiLastPeak.PeakType} exit was at {rsiLastPeak.ExitTrade.DateTime:t} - {(DateTime.Now - rsiLastPeak.ExitTrade.DateTime).TotalMinutes} minutes ago");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
