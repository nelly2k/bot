using System;
using System.Linq;
using bot.core.Extensions;
using bot.model;
using FizzWare.NBuilder;
using NUnit.Framework;

namespace bot.core.tests
{
    public class WhenSearchForPeaks
    {
        [Test]
        public void TwoPeaks()
        {
            var dt = new DateTime(2015, 05, 05);
            var data = Builder<BaseTrade>.CreateListOfSize(27)
                .TheFirst(1).With(x => x.DateTime = dt).With(x => x.Price = 20)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(2)).With(x => x.Price = 30)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(3)).With(x => x.Price = 40)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(4)).With(x => x.Price = 50)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(5)).With(x => x.Price = 65)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(6)).With(x => x.Price = 70)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(7)).With(x => x.Price = 75)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(8)).With(x => x.Price = 50)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(9)).With(x => x.Price = 40)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(10)).With(x => x.Price = 40)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(11)).With(x => x.Price = 40)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(12)).With(x => x.Price = 50)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(13)).With(x => x.Price = 70)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(14)).With(x => x.Price = 65)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(15)).With(x => x.Price = 60)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(16)).With(x => x.Price = 55)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(17)).With(x => x.Price = 52)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(18)).With(x => x.Price = 45)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(19)).With(x => x.Price = 30)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(20)).With(x => x.Price = 20)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(21)).With(x => x.Price = 18)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(22)).With(x => x.Price = 16)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(23)).With(x => x.Price = 12)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(24)).With(x => x.Price = 10)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(25)).With(x => x.Price = 15)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(26)).With(x => x.Price = 22)
                .TheNext(1).With(x => x.DateTime = dt.AddMinutes(27)).With(x => x.Price = 25)
                .Build();

            var result = data.GetPeaks(20, 50).ToArray();
            
            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result[0].PeakTrade.DateTime, Is.EqualTo(dt.AddMinutes(24)));
            Assert.That(result[0].PeakTrade.Price, Is.EqualTo(10));
            Assert.That(result[0].ExitTrade.Price, Is.EqualTo(15));
            Assert.That(result[0].ExitTrade.DateTime, Is.EqualTo(dt.AddMinutes(25)));
            Assert.That(result[0].PeakType, Is.EqualTo(PeakType.Low));

            Assert.That(result[1].PeakTrade.DateTime, Is.EqualTo(dt.AddMinutes(13)));
            Assert.That(result[1].PeakTrade.Price, Is.EqualTo(70));
            Assert.That(result[1].ExitTrade.DateTime, Is.EqualTo(dt.AddMinutes(17)));
            Assert.That(result[1].ExitTrade.Price, Is.EqualTo(52));
            Assert.That(result[1].PeakType, Is.EqualTo(PeakType.High));

            Assert.That(result[2].PeakTrade.DateTime, Is.EqualTo(dt.AddMinutes(7)));
            Assert.That(result[2].PeakTrade.Price, Is.EqualTo(75));
            Assert.That(result[2].ExitTrade.DateTime, Is.EqualTo(dt.AddMinutes(7)));
            Assert.That(result[2].ExitTrade.Price, Is.EqualTo(75));
            Assert.That(result[2].PeakType, Is.EqualTo(PeakType.High));
        }
    }
}