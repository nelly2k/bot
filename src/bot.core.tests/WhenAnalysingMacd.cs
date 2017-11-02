using System;
using bot.core.Extensions;
using FizzWare.NBuilder;
using NUnit.Framework;

namespace bot.core.tests
{
    public class WhenAnalysingMacd
    {
        [Test]
        public void MacdRisesSignal()
        {
            var date = new DateTime(2015, 6, 7);
            var data = Builder<MacdResultItem>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date).With(x => x.Macd = 10).With(x => x.Signal = 15)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(10)).With(x => x.Macd = 14).With(x => x.Signal = 12)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(20)).With(x => x.Macd = 15).With(x => x.Signal = 10)
                .Build();

            var macdAnalResult = data.MacdAnalysis();

            Assert.That(macdAnalResult.CrossType, Is.EqualTo(CrossType.MacdRises));
            Assert.That(macdAnalResult.Trade.DateTime, Is.EqualTo(date.AddMinutes(10)));
        }

        [Test]
        public void MacdFallsSignal()
        {
            var date = new DateTime(2015, 6, 7);
            var data = Builder<MacdResultItem>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date).With(x => x.Macd = 15).With(x => x.Signal = 10)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(10)).With(x => x.Macd = 12).With(x => x.Signal = 14)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(20)).With(x => x.Macd = 10).With(x => x.Signal = 15)
                .Build();

            var macdAnalResult = data.MacdAnalysis();

            Assert.That(macdAnalResult.CrossType, Is.EqualTo(CrossType.MacdFalls));
            Assert.That(macdAnalResult.Trade.DateTime, Is.EqualTo(date.AddMinutes(10)));
        }

        [Test]
        public void NotCross()
        {
            var date = new DateTime(2015, 6, 7);
            var data = Builder<MacdResultItem>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date).With(x => x.Macd = 15).With(x => x.Signal = 10)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(10)).With(x => x.Macd = 12).With(x => x.Signal = 9)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(20)).With(x => x.Macd = 11).With(x => x.Signal = 5)
                .Build();

            var macdAnalResult = data.MacdAnalysis();

            Assert.That(macdAnalResult.CrossType, Is.Null);
            Assert.That(macdAnalResult.Trade, Is.Null);
        }

        [Test]
        public void SlowAnalysis_MacdOverSignal_Buy()
        {
            var date = new DateTime(2015, 6, 7);
            var data = Builder<MacdResultItem>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date.AddMinutes(1)).With(x => x.Macd = 10).With(x => x.Signal = 5)
                .TheNext(2).With(x => x.DateTime = date).With(x => x.Macd = 5).With(x => x.Signal = 10)
                .Build();

            var result = data.MacdSlowAnalysis();
            Assert.That(result, Is.EqualTo(TradeStatus.Buy));
        }

        [Test]
        public void SlowAnalysis_SignalOverMacd_Unknown()
        {
            var date = new DateTime(2015, 6, 7);
            var data = Builder<MacdResultItem>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date.AddMinutes(1)).With(x => x.Macd = 5).With(x => x.Signal = 10)
                .TheNext(2).With(x => x.DateTime = date).With(x => x.Macd = 5).With(x => x.Signal = 10)
                .Build();

            var result = data.MacdSlowAnalysis();
            Assert.That(result, Is.EqualTo(TradeStatus.Unknown));
        }

        [Test]
        public void SlowAnalysis_MacdDifferentToSignal()
        {
            var date = new DateTime(2015, 6, 7);
            var data = Builder<MacdResultItem>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date.AddMinutes(1)).With(x => x.Macd = 5).With(x => x.Signal = 6)
                .TheNext(2).With(x => x.DateTime = date).With(x => x.Macd = 5).With(x => x.Signal = 10)
                .Build();

            var result = data.MacdSlowAnalysis(1);
            Assert.That(result, Is.EqualTo(TradeStatus.Buy));
        }

        [Test]
        public void SlowAnalysis_BelowSignalButRising_Unknown()
        {
            var date = new DateTime(2015, 6, 7);
            var data = Builder<MacdResultItem>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date.AddMinutes(2)).With(x => x.Macd = 3).With(x => x.Signal = 4)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(1)).With(x => x.Macd = 2).With(x => x.Signal = 5)
                .TheNext(1).With(x => x.DateTime = date).With(x => x.Macd = 1).With(x => x.Signal =6)
                .Build();

            var result = data.MacdSlowAnalysis(0);
            Assert.That(result, Is.EqualTo(TradeStatus.Buy));
        }

        [Test]
        public void SlowAnalysis_Various_Unknown()
        {
            var date = new DateTime(2015, 6, 7);
            var data = Builder<MacdResultItem>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date.AddMinutes(2)).With(x => x.Macd = 3).With(x => x.Signal = 4)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(1)).With(x => x.Macd = 2).With(x => x.Signal = 5)
                .TheNext(1).With(x => x.DateTime = date).With(x => x.Macd = 2.5m).With(x => x.Signal = 4.5m)
                .Build();

            var result = data.MacdSlowAnalysis(0);
            Assert.That(result, Is.EqualTo(TradeStatus.Unknown));
        }
    }
}