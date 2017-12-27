using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bot.core.Extensions;
using bot.model;
using NUnit.Framework;

namespace bot.core.tests
{
    public class WhenAnalysingIndeces
    {

        [Test]
        public void Long_Macd_FRises_SBuy_RSI_Low_Price_Rises_Buy()
        {
            var dt = new DateTime(2017,10,10);
            var trade = new BaseTrade() {DateTime = dt.AddMinutes(1)};
            var macd_fast = new MacdAnalysisResult()
            {
                CrossType = CrossType.MacdRises,
                Trade = trade
            };

            var rsi  = new PeakAnalysisResult()
            {
             ExitTrade   = trade,
             PeakType = PeakType.Low
            };


            var macdSlowStatus = TradeStatus.Buy;

            var result = AnalysisExtensions.AnalyseIndeces(30, dt, macd_fast, rsi, macdSlowStatus);
            Assert.That(result, Is.EqualTo(PriceStatus.GoinRaise));
        }

        [Test]
        public void Long_Macd_FFalls_SDontCare_RSI_High_Price_Falling_Sell()
        {
            var dt = new DateTime(2017, 10, 10);
            var trade = new BaseTrade() { DateTime = dt.AddMinutes(1) };
            var macd_fast = new MacdAnalysisResult()
            {
                CrossType = CrossType.MacdFalls,
                Trade = trade
            };

            var rsi = new PeakAnalysisResult()
            {
                ExitTrade = trade,
                PeakType = PeakType.High
            };


            var macdSlowStatus = TradeStatus.Buy;

            var result = AnalysisExtensions.AnalyseIndeces(30, dt, macd_fast, rsi, macdSlowStatus);
            Assert.That(result, Is.EqualTo(PriceStatus.GoingFall));
        }
    }
}
