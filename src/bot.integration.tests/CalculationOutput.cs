﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bot.core;
using bot.core.Extensions;
using bot.kraken;
using bot.model;
using NUnit.Framework;

namespace bot.integration.tests
{
    public class CalculationOutput
    {
        private TradeRepository _tradeRepository;
        private DateTime _dt;
        private const string AltName = "ETHUSD";

        [SetUp]
        public void Setup()
        {
            _tradeRepository = new TradeRepository();
            _dt = DateTime.Now.AddHours(-12);
        }

        [Test]
        public async Task  AllTogether()
        {
            var configRepo = new ConfigRepository(new List<IExchangeConfig>{new KrakenConfig()}.ToArray());

            var config = await configRepo.Get();
            config[AltName].GroupMinutes = 15;
            config[AltName].GroupForLongMacdMinutes = 60;
            _dt = DateTime.Now.AddHours(-26);
            var trades = (await _tradeRepository.LoadTrades(AltName, _dt)).ToList();
            var grouped = trades.GroupAll(config[AltName].GroupMinutes, GroupBy.Minute).ToList();
            var macd = grouped.Macd(config[AltName].MacdSlow, config[AltName].MacdFast, config[AltName].MacdSignal).ToList();
            var rsi = grouped.RelativeStrengthIndex(config[AltName].RsiEmaPeriods);
            
            var grouped2 = trades.GroupAll(config[AltName].GroupForLongMacdMinutes, GroupBy.Minute).ToList();
            var macd2 = grouped2.Macd(config[AltName].MacdSlow, config[AltName].MacdFast, config[AltName].MacdSignal).ToList();

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
        public async Task LoadTradesTask()
        {
            var repo = new TradeRepository();
            var result= await repo.LoadTrades("XBTUSD", new DateTime(2017, 11, 02, 11, 40, 00),
                new DateTime(2017, 11, 03, 11, 40, 00));
            Assert.That(result.Any(), Is.True);
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
