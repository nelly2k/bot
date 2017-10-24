using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;
using bot.core.Extensions;
using bot.model;
using Combinatorics.Collections;
using NUnit.Framework;

namespace bot.core.tests
{
    public class Integration
    {
        private TradeRepository _tradeRepository;
        private DateTime _dt;
        private const string AltName = "XETHZUSD";

        [SetUp]
        public void Setup()
        {
            _tradeRepository = new TradeRepository();
            _dt = DateTime.Now.AddHours(-12);
        }

        [Test]
        public async Task  AllTogether()
        {
            var configRepo = new ConfigRepository();

            var config = await configRepo.Get();

            _dt = DateTime.Now.AddHours(-20);
            var trades = (await _tradeRepository.LoadTrades(AltName, _dt)).ToList();
            var grouped = trades.GroupAll(config.AnalyseGroupPeriodMinutes, GroupBy.Minute).ToList();
            var macd = grouped.Macd(config.AnalyseMacdSlow, config.AnalyseMacdFast, config.AnalyseMacdSignal).ToList();
            var rsi = grouped.RelativeStrengthIndex(config.AnalyseRsiEmaPeriods);
            
            var grouped2 = trades.GroupAll(20, GroupBy.Minute).ToList();
            var macd2 = grouped2.Macd(config.AnalyseMacdSlow, config.AnalyseMacdFast, config.AnalyseMacdSignal).ToList();

            var lines = (from m in macd
                         join r in rsi on m.DateTime equals r.DateTime
                         join g in grouped on m.DateTime equals g.DateTime
                         join m2 in macd2 on m.DateTime equals m2.DateTime into gm2
                         from m2 in gm2.DefaultIfEmpty()
                         select $"{m.DateTime},{r.Price},{m.Macd},{m.Signal},{g.Volume},{g.Price},{g.PriceMin},{g.PriceMax},{m2?.Macd},{m2?.Signal}").ToList();

            lines.Insert(0, "Date,RSI,MACD,Signal,Volume,PriceAvg,PriceMin,PriceMax,MACD2,Signal2");

            Write("macd_rsi", lines);
        }


        [Test]
        public async Task Trail()
        {
            var result = new List<TradeEvent>();
            var start = new DateTime(2017, 09, 29, 15, 0, 0);
            var end = new DateTime(2017, 10, 05, 8, 00, 0);

            var db = new TradeRepository();
            var cofigRepo = new ConfigRepository();
            var config = await cofigRepo.Get();

            var currentTime = start;
            var lines = new List<string>();
            lines.Add("Date,Status,PriceAvg,PriceSell,PriceBuy");
            while (currentTime <= end)
            {
                var trades = await db.LoadTrades("XETHZUSD", currentTime.AddHours(-config.AnalyseLoadHours), currentTime);

                var grouped = trades.GroupAll(config.AnalyseGroupPeriodMinutes, GroupBy.Minute).ToList();
                var macd = grouped.Macd(config.AnalyseMacdSlow, config.AnalyseMacdFast, config.AnalyseMacdSignal).MacdAnalysis();
                var rsi = grouped.RelativeStrengthIndex(config.AnalyseRsiEmaPeriods);
                var rsiLastPeak = rsi.GetPeaks(config.AnalyseRsiLow, config.AnalyseRsiHigh).OrderByDescending(x => x.PeakTrade.DateTime)
                    .FirstOrDefault();
                var newStatus = AnalysisExtensions.AnalyseIndeces(config.AnalyseTresholdMinutes, currentTime, macd, rsiLastPeak);
                var price = grouped.First(x => x.DateTime == macd.Trade.DateTime);
                if (newStatus != TradeStatus.Unknown && (!result.Any() || newStatus != result.Last().TradeStatus))
                {
                    var actualPrice = newStatus == TradeStatus.Buy ? price.PriceBuyAvg : price.PriceSellAvg;
                    lines.Add($"{macd.Trade.DateTime},{newStatus},{price.Price:C},{price.PriceSellAvg:C},{price.PriceBuyAvg:C}");
                    result.Add(new TradeEvent
                    {
                        DateTime = macd.Trade.DateTime,
                        Price = actualPrice,
                        TradeStatus = newStatus
                    });
                }

                currentTime = currentTime.AddMinutes(config.LoadIntervalMinutes);
            }
            Write("trail_", lines);
        }

        //[Test]
        //public async Task RunOneTrail()
        //{
        //    var startFall = new DateTime(2017, 10, 07, 00, 0, 0);
        //    var end = new DateTime(2017, 10, 08, 8, 00, 0);
        //    var db = new TradeRepository();
        //    var config = await db.GetConfig();

        //    var result = await RunConfigTrail(config, 65m, startFall, end);

        //}

        //[Test]
        //public async Task RunTrail()
        //{
        //    var initialUsd = 65m;
        //    var filename= Write("full_trail", true,"Type,LoadInterval,LoadHours,GroupPeriod,Treshold,MacdSlow,MacdFast,MacdSignal,RsiPeriods,RsiLow,RsiHigh,Amount,Buys,Sells");
        //    var config = new Config();

        //    var values = new Dictionary<string, int[]>()
        //    {
        //        {nameof(config.LoadIntervalMinutes),new []{3,5,7}},
        //        {nameof(config.AnalyseGroupPeriodMinutes),new []{3,4,5}},
        //        {nameof(config.AnalyseTresholdMinutes),new []{10,20,30} },
        //        {nameof(config.AnalyseMacdSlow),new []{20,26,30} },
        //        {nameof(config.AnalyseMacdFast),new []{10,12,15} },
        //        {nameof(config.AnalyseMacdSignal),new []{5,9,10} },
        //        {nameof(config.AnalyseRsiEmaPeriods),new []{8,14,16} },
        //        {nameof(config.AnalyseRsiLow),new []{20,30,35} },
        //        {nameof(config.AnalyseRsiHigh),new []{80,70,65} },
        //    };

        //    var start = new DateTime(2017, 9, 29, 15, 0, 0);
        //    var endRise = new DateTime(2017, 9, 30, 23, 0, 0);
        //    var end = new DateTime(2017, 10, 05, 8, 00, 0);

        //    var allConfigurations = from AnalyseGroupPeriodMinutes in values[nameof(config.AnalyseGroupPeriodMinutes)]
        //                            from LoadIntervalMinutes in values[nameof(config.LoadIntervalMinutes)]
        //                            from AnalyseTresholdMinutes in values[nameof(config.AnalyseTresholdMinutes)]
        //                            from AnalyseMacdSlow in values[nameof(config.AnalyseMacdSlow)]
        //                            from AnalyseMacdFast in values[nameof(config.AnalyseMacdFast)]
        //                            from AnalyseMacdSignal in values[nameof(config.AnalyseMacdSignal)]
        //                            from AnalyseRsiEmaPeriods in values[nameof(config.AnalyseRsiEmaPeriods)]
        //                            from AnalyseRsiLow in values[nameof(config.AnalyseRsiLow)]
        //                            from AnalyseRsiHigh in values[nameof(config.AnalyseRsiHigh)]
        //                            select new Config()
        //                            {
        //                                AnalyseGroupPeriodMinutes = AnalyseGroupPeriodMinutes,
        //                                AnalyseTresholdMinutes = AnalyseTresholdMinutes,
        //                                AnalyseMacdSlow = AnalyseMacdSlow,
        //                                AnalyseMacdFast = AnalyseMacdFast,
        //                                AnalyseMacdSignal = AnalyseMacdSignal,
        //                                AnalyseRsiEmaPeriods = AnalyseRsiEmaPeriods,
        //                                AnalyseRsiLow = AnalyseRsiLow,
        //                                AnalyseRsiHigh = AnalyseRsiHigh,
        //                                LoadIntervalMinutes = LoadIntervalMinutes,
        //                            };


        //    foreach (var configValue in allConfigurations)
        //    {
        //        await Item("All", initialUsd, configValue, start, end, str=>Write(filename, false,str));
        //        await Item("Rise", initialUsd, configValue, start, endRise, str => Write(filename, false, str));
        //        await Item("Fall", initialUsd, configValue, endRise, end, str => Write(filename, false, str));
        //    }
        //}

        //private async Task Item(string type, decimal initialUsd, Config config, DateTime start, DateTime end, Action<string> add)
        //{
        //    try
        //    {
        //        var trail = await RunConfigTrail(config, initialUsd, start, end);
        //        add(GetLine(type, initialUsd, config, trail));
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //    }

        //}

        private static string GetLine(string type, decimal initialUsd, Config config, ConfigurationTrailResult trailResult)
        {
            return
                $"{type},{initialUsd},{config.LoadIntervalMinutes},{config.AnalyseLoadHours},{config.AnalyseGroupPeriodMinutes},{config.AnalyseTresholdMinutes}" +
                $",{config.AnalyseMacdSlow},{config.AnalyseMacdFast},{config.AnalyseMacdSignal},{config.AnalyseRsiEmaPeriods},{config.AnalyseRsiLow}," +
                $"{config.AnalyseRsiHigh},{trailResult.Price:C},{trailResult.BuyNum},{trailResult.SellNum}";
        }

        //private async Task<ConfigurationTrailResult> RunConfigTrail(Config config, decimal usd, DateTime start, DateTime end)
        //{
        //    var db = new TradeRepository();
        //    var result = new ConfigurationTrailResult();
        //    var core = new TradeService();
        //    var currentTime = start;
          
        //    var currentUsd = usd;
        //    var currentEth = decimal.Zero;
        //    var price = decimal.Zero;
        //    TradeStatus? lastStatus = null;
        //    while (currentTime <= end)
        //    {
        //        var trades = await db.LoadTrades("XETHZUSD", currentTime.AddHours(-config.AnalyseLoadHours), currentTime);

        //        try
        //        {
        //            var grouped = trades.GroupAll(config.AnalyseGroupPeriodMinutes, GroupBy.Minute).ToList();
        //            var macd = grouped.Macd(config.AnalyseMacdSlow, config.AnalyseMacdFast, config.AnalyseMacdSignal).MacdAnalysis();
        //            var rsi = grouped.RelativeStrengthIndex(config.AnalyseRsiEmaPeriods);
        //            var rsiLastPeak = rsi.GetPeaks(config.AnalyseRsiLow, config.AnalyseRsiHigh).OrderByDescending(x => x.PeakTrade.DateTime)
        //                .FirstOrDefault();
        //            var newStatus = AnalysisExtensions.AnalyseIndeces(config.AnalyseTresholdMinutes, currentTime, macd, rsiLastPeak);
        //            var trade = grouped.FirstOrDefault(x => x.DateTime == macd.Trade.DateTime);
        //            if (trade == null)
        //            {
        //                throw new Exception($"Trade is not identified. {config}");
        //            }
        //            if (newStatus != TradeStatus.Unknown && (lastStatus == null || newStatus != lastStatus))
        //            {
        //                lastStatus = newStatus;
        //                if (newStatus == TradeStatus.Buy)
        //                {
        //                    price = Math.Round(trade.PriceBuyAvg == decimal.Zero ? trade.Price : trade.PriceBuyAvg, 2);
        //                    var fee = core.Transform(usd, price, 0.26m);
        //                   // currentEth = fee.TargetCurrencyAmount;
        //                    currentUsd = fee.BaseCurrencyRest;
        //                    result.BuyNum += 1;
        //                }
        //                else if (currentEth != decimal.Zero)
        //                {
        //                    price = trade.PriceSellAvg == decimal.Zero ? trade.Price : trade.PriceSellAvg;
                            
        //                    var fee = core.Transform(currentEth, price, 0.16m, FeeSource.Target);
        //                    currentUsd = fee.TargetCurrencyAmount;
        //                   // currentEth = fee.BaseCurrencyRest;
        //                    result.SellNum += 1;
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine($"{config}{Environment.NewLine}{e}");
        //            return result;
        //        }

        //        currentTime = currentTime.AddMinutes(config.LoadIntervalMinutes);
        //    }

        //    result.Price = currentEth * price + currentUsd;
        //    return result;
        //}

    


        private class ConfigurationTrailResult
        {
            public int BuyNum { get; set; }
            public int SellNum { get; set; }
            public decimal Price { get; set; }
        }

        private class TradeEvent
        {
            public decimal Price { get; set; }
            public TradeStatus TradeStatus { get; set; }
            public DateTime DateTime { get; set; }
        }

        [Test]
        public async Task AllTogetherCertainPeriod()
        {
            var configRepo = new ConfigRepository();

            var config = await configRepo.Get();
            var end = new DateTime(2017, 10, 20, 20, 0, 0);

            var start = end.AddHours(-18);

            var trades = await _tradeRepository.LoadTrades(AltName, start, end);
            var grouped = trades.GroupAll(config.AnalyseGroupPeriodMinutes, GroupBy.Minute).ToList();
            var macd = grouped.Macd(config.AnalyseMacdSlow, config.AnalyseMacdFast, config.AnalyseMacdSignal).ToList();
            var rsi = grouped.RelativeStrengthIndex(config.AnalyseRsiEmaPeriods).ToList();
            var macdAnal = macd.MacdAnalysis();
            var rsiLastPeak = rsi.GetPeaks(config.AnalyseRsiLow, config.AnalyseRsiHigh).OrderByDescending(x => x.PeakTrade.DateTime)
                .FirstOrDefault();

            var lines = (from m in macd
                         join r in rsi on m.DateTime equals r.DateTime
                         join g in grouped on m.DateTime equals g.DateTime
                         select $"{m.DateTime},{r.Price},{m.Macd},{m.Signal},{g.Volume},{g.Price},{g.PriceMin},{g.PriceMax},{Mark(macdAnal, rsiLastPeak, m.DateTime)}").ToList();

            lines.Insert(0, "Date,RSI,MACD,Signal,Volume,PriceAvg,PriceMin,PriceMax,MACD_Anal,RSI_Anal");

            Write("macd_rsi_period", lines);
           // var newStatus = AnalysisExtensions.AnalyseIndeces(20, end, macdAnal, rsiLastPeak);

         //   Assert.That(newStatus, Is.EqualTo(TradeStatus.Sell));
        }

        private static string Mark(MacdAnalysisResult macd, PeakAnalysisResult rsi, DateTime dt)
        {
            var macdVal = dt == macd.Trade.DateTime ? macd.Trade.Price.ToString() : "";
            var rsiVal = dt == rsi.ExitTrade.DateTime ? rsi.ExitTrade.Price.ToString() : "";

            return $"{macdVal},{rsiVal}";
        }

        public void Write(string name, IEnumerable<string> lines)
        {
            using (var file = new System.IO.StreamWriter($"h:\\{name}_{DateTime.Now:yyMMddhhmmss}.csv", true))
            {
                foreach (var line in lines)
                {
                    file.WriteLine(line);
                }
                file.Close();
            }
        }
        public string Write(string name, bool isNewFile, params string[] lines)
        {
            var filename = isNewFile ? $"h:\\{name}_{DateTime.Now:yyMMddhhmmss}.csv" : name;
            using (var file = new System.IO.StreamWriter(filename, true))
            {
                foreach (var line in lines)
                {
                    file.WriteLine(line);
                }
                file.Close();
            }
            return filename;
        }
    }
}
