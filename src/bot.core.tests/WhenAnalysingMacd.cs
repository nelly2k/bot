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
    }
}