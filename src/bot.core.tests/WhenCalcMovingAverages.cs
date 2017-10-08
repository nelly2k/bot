using System.Linq;
using bot.core.Extensions;
using bot.kraken;
using bot.kraken.Model;
using FizzWare.NBuilder;
using NUnit.Framework;

namespace bot.core.tests
{
    public class WhenCalcMovingAverages
    {
        [Test]
        public void Sma()
        {
            var data = Builder<Trade>.CreateListOfSize(7)
                .TheFirst(1).With(x => x.Price = 11)
                .TheNext(1).With(x => x.Price = 12)
                .TheNext(1).With(x => x.Price = 13)
                .TheNext(1).With(x => x.Price = 14)
                .TheNext(1).With(x => x.Price = 15)
                .TheNext(1).With(x => x.Price = 16)
                .TheNext(1).With(x => x.Price = 17)
                .Build();

            var sma= data.SimpleMovingAverage(5);
            Assert.That(sma.ElementAt(0).Price,Is.EqualTo(13));
            Assert.That(sma.ElementAt(1).Price,Is.EqualTo(14));
            Assert.That(sma.ElementAt(2).Price,Is.EqualTo(15));
        }

        [Test]
        public void Ema()
        {
            var data = new[] {22.27, 22.19, 22.08,22.17,22.18,22.13,22.23,22.43,22.43,22.24,22.29,22.15,22.39,22.38,22.61}.Select(x=>new Trade()
            {
                Price = (decimal)x
            }).AsEnumerable();

            var ema = data.ExponentialMovingAverage(10);
            Assert.That(ema.ElementAt(0).Price, Is.EqualTo(22.21).Within(0.01));
            Assert.That(ema.ElementAt(1).Price, Is.EqualTo(22.25).Within(0.01));
            Assert.That(ema.ElementAt(2).Price, Is.EqualTo(22.27).Within(0.01));
            Assert.That(ema.ElementAt(3).Price, Is.EqualTo(22.33).Within(0.01));

        }

        [Test]
        public void Rsi()
        {
            var data = new[]
            {
                44.34,44.09,44.15,43.61,44.33,44.83,45.10,45.42,45.82,46.08,45.89,46.03,45.61,46.28,46.28,46,46.03,46.41
                ,46.22,45.64,46.21,46.25,45.71,46.45,45.78,45.35,44.03,44.18,44.22,44.57,43.42,42.66                
            }.Select(x => new Trade()
            {
                Price = (decimal)x
            }).AsEnumerable();
            var rsi = data.RelativeStrengthIndex().ToArray();
            Assert.That(rsi[0].Price,Is.EqualTo(70.86).WithSameOffset);
        }

        [Test]
        public void Change()
        {

            var data = new[] { 44.34, 44.09}.Select(x => new Trade
            {
                Price = (decimal)x
            }).AsEnumerable();
            var change = data.Change();
            Assert.That(change.Single().Price, Is.EqualTo(-0.25).Within(0.01));
        }

    }
}