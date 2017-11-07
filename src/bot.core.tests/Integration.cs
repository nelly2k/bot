using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bot.core.Extensions;
using NUnit.Framework;

namespace bot.core.tests
{
    public class Integration
    {
        private TradeRepository _tradeRepository;
        private DateTime _dt;
        private const string AltName = "XBTUSD";

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
            config.AnalyseGroupPeriodMinutes = 15;
            _dt = DateTime.Now.AddHours(-12);
            var trades = (await _tradeRepository.LoadTrades(AltName, _dt)).ToList();
            var grouped = trades.GroupAll(config.AnalyseGroupPeriodMinutes, GroupBy.Minute).ToList();
            var macd = grouped.Macd(config.AnalyseMacdSlow, config.AnalyseMacdFast, config.AnalyseMacdSignal).ToList();
            var rsi = grouped.RelativeStrengthIndex(config.AnalyseRsiEmaPeriods);
            
            var grouped2 = trades.GroupAll(config.AnalyseMacdGroupPeriodMinutesSlow, GroupBy.Minute).ToList();
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
           
        }

        [Test]
        public async Task LoadTradesTask()
        {
            var repo = new TradeRepository();
            var result= await repo.LoadTrades("XBTUSD", new DateTime(2017, 11, 02, 11, 40, 00),
                new DateTime(2017, 11, 03, 11, 40, 00));
            Assert.That(result.Any(), Is.True);
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
