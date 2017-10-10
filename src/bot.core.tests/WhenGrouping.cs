using System;
using System.Linq;
using bot.core.Extensions;
using bot.kraken;
using bot.kraken.Model;
using FizzWare.NBuilder;
using NUnit.Framework;

namespace bot.core.tests
{
    public class WhenGrouping   
    {
        [Test]
        public void By10Minutes()
        {
            var date = new DateTime(2015,5,5,10,0,0);
            var data = Builder<KrakenTrade>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date).With(x=>x.Price = 1)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(5)).With(x => x.Price = 2)
                .TheLast(1).With(x => x.DateTime = date.AddMinutes(11)).With(x => x.Price = 4)
                .Build();

            var result = data.GroupPrice(10, GroupBy.Minute, Operation.Sum).ToList();
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.First().DateTime, Is.EqualTo(date));
            Assert.That(result.Last().DateTime, Is.EqualTo(date.AddMinutes(10)));
        }

        [Test]
        public void Operation_Sum()
        {
            var date = new DateTime(2015, 5, 5, 10, 0, 0);
            var data = Builder<KrakenTrade>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date).With(x => x.Price = 1)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(5)).With(x => x.Price = 2)
                .TheLast(1).With(x => x.DateTime = date.AddMinutes(11)).With(x => x.Price = 4)
                .Build();

            var result = data.GroupPrice(10, GroupBy.Minute, Operation.Sum).ToList();
            Assert.That(result.First().Price, Is.EqualTo(3));
            Assert.That(result.Last().Price, Is.EqualTo(4));
        }

        [Test]
        public void Operation_Max()
        {
            var date = new DateTime(2015, 5, 5, 10, 0, 0);
            var data = Builder<KrakenTrade>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date).With(x => x.Price = 1)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(5)).With(x => x.Price = 2)
                .TheLast(1).With(x => x.DateTime = date.AddMinutes(11)).With(x => x.Price = 4)
                .Build();

            var result = data.GroupPrice(10, GroupBy.Minute, Operation.Max).ToList();
            Assert.That(result.First().Price, Is.EqualTo(2));
        }

        [Test]
        public void Operation_Min()
        {
            var date = new DateTime(2015, 5, 5, 10, 0, 0);
            var data = Builder<KrakenTrade>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date).With(x => x.Price = 1)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(5)).With(x => x.Price = 2)
                .TheLast(1).With(x => x.DateTime = date.AddMinutes(11)).With(x => x.Price = 4)
                .Build();

            var result = data.GroupPrice(10, GroupBy.Minute, Operation.Min).ToList();
            Assert.That(result.First().Price, Is.EqualTo(1));
        }

        [Test]
        public void Operation_Average()
        {
            var date = new DateTime(2015, 5, 5, 10, 0, 0);
            var data = Builder<KrakenTrade>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date).With(x => x.Price = 1)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(5)).With(x => x.Price = 2)
                .TheLast(1).With(x => x.DateTime = date.AddMinutes(11)).With(x => x.Price = 4)
                .Build();

            var result = data.GroupPrice(10, GroupBy.Minute, Operation.Average).ToList();
            Assert.That(result.First().Price, Is.EqualTo(1.5));
        }


        [Test]
        public void By30Minutes()
        {
            var date = new DateTime(2015, 5, 5, 10, 0, 0);
            var data = Builder<KrakenTrade>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(28))
                .TheLast(1).With(x => x.DateTime = date.AddMinutes(35))
                .Build();

            var result = data.GroupPrice(30, GroupBy.Minute, Operation.Sum).ToList();
            Console.WriteLine(result.Last().DateTime.ToString());
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.First().DateTime, Is.EqualTo(date));
            Assert.That(result.Last().DateTime, Is.EqualTo(date.AddMinutes(30)));
        }

        [Test]
        public void By1Hour()
        {
            var date = new DateTime(2015, 5, 5, 10, 0, 0);
            var data = Builder<KrakenTrade>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(58))
                .TheLast(1).With(x => x.DateTime = date.AddMinutes(61))
                .Build();

            var result = data.GroupPrice(1, GroupBy.Hour, Operation.Sum).ToList();
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.First().DateTime, Is.EqualTo(date));
            Assert.That(result.Last().DateTime, Is.EqualTo(date.AddHours(1)));
        }

        [Test]
        public void GroupVolume()
        {
            var date = new DateTime(2015, 5, 5, 10, 0, 0);
            var data = Builder<KrakenTrade>.CreateListOfSize(3)
                .TheFirst(1).With(x => x.DateTime = date).With(x=>x.Volume = (decimal)0.001)
                .TheNext(1).With(x => x.DateTime = date.AddMinutes(30)).With(x => x.Volume = (decimal)0.001)
                .TheLast(1).With(x => x.DateTime = date.AddMinutes(45)).With(x => x.Volume = (decimal)0.001)
                .Build();

            var result = data.GroupAll(1, GroupBy.Hour).ToArray();

            Assert.That(result[0].Volume, Is.EqualTo(0.003).Within(0.0001));
        }
    }
}
