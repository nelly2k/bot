using System;
using System.Collections.Generic;
using System.Linq;
using bot.model;

namespace bot.core.Extensions
{
    public static class CalculationExtensions
    {

        public static IEnumerable<IDateCost> SimpleMovingAverage(this IEnumerable<IDateCost> list, int num)
        {
            if (list.Count() <= num)
            {
                throw new Exception("Cannot calculate SMA, data is insufficient.");
            }
            var queue = new Queue<decimal>();

            foreach (var trade in list)
            {
                if (queue.Count >= num)
                {
                    queue.Dequeue();
                }

                queue.Enqueue(trade.Price);

                if (queue.Count >= num)
                {
                    yield return new BaseTrade
                    {
                        DateTime = trade.DateTime,
                        Price = queue.Average()
                    };
                }
            }
        }

        public static IEnumerable<IDateCost> ExponentialMovingAverage(this IEnumerable<IDateCost> list, int num)
        {
            if (list.Count() <= num)
            {
                throw new Exception("Cannot calculate EMA, data is insufficient.");
            }
            var smoothing = 2 / ((decimal)num + 1);
            var prev = list.Take(num).Average(x => x.Price);


            for (var i = num + 1; i < list.Count(); i++)
            {
                var trade = list.ElementAt(i);
                var ema = smoothing * (trade.Price - prev) + prev;
                yield return new BaseTrade
                {
                    DateTime = trade.DateTime,
                    Price = ema
                };
                prev = ema;
            }
        }

        public static IEnumerable<IDateCost> Change(this IEnumerable<IDateCost> list)
        {
            for (var i = 1; i < list.Count(); i++)
            {
                var me = list.ElementAt(i);
                yield return new BaseTrade()
                {
                    Price = me.Price - list.ElementAt(i - 1).Price,
                    DateTime = me.DateTime
                };
            }
        }

        public static IEnumerable<IDateCost> RelativeStrengthIndex(this IEnumerable<IDateCost> list, int num = 14, MovingAverageStyle movingAverageStyle = MovingAverageStyle.EMA)
        {
            var change = list.Change().ToList();
            var gain = change.Select(x => x.Price > 0 ? x : new BaseTrade { DateTime = x.DateTime, Price = decimal.Zero }).ToArray();
            var loss = change.Select(x => x.Price < 0 ? x : new BaseTrade { DateTime = x.DateTime, Price = decimal.Zero }).ToArray();
            switch (movingAverageStyle)
            {
                case MovingAverageStyle.SMA:
                    gain = gain.SimpleMovingAverage(num).ToArray();
                    loss = loss.SimpleMovingAverage(num).ToArray();
                    break;
                case MovingAverageStyle.EMA:
                    gain = gain.ExponentialMovingAverage(num).ToArray();
                    loss = loss.ExponentialMovingAverage(num).ToArray();
                    break;
            }

            for (var i = 0; i < gain.Length; i++)
            {
                decimal rsi;
                if (loss[i].Price == decimal.Zero)
                {
                    rsi = 100;
                }
                else
                {
                    var rs = gain[i].Price / Math.Abs(loss[i].Price);
                    rsi = 100 - (100 / (1 + rs));
                }
              
                yield return new BaseTrade
                {
                    DateTime = gain[i].DateTime,
                    Price = rsi
                };
            }
        }

        public static IEnumerable<MacdResultItem> Macd(this IEnumerable<IDateCost> list, int macdSlow, int macdFast,
            int signalNum)
        {
            var slow = list.ExponentialMovingAverage(macdSlow);
            var fast = list.ExponentialMovingAverage(macdFast);

            var macd = (from s in slow
                        join f in fast on s.DateTime equals f.DateTime
                        select new BaseTrade()
                        {
                            DateTime = s.DateTime,
                            Price = f.Price - s.Price
                        }).ToList();

            var signal = macd.ExponentialMovingAverage(signalNum);


            return from m in macd
                   join s in signal on m.DateTime equals s.DateTime
                   select new MacdResultItem()
                   {
                       DateTime = s.DateTime,
                       Macd = m.Price,
                       Signal = s.Price
                   };
        }
    }

    public class MacdResultItem
    {
        public DateTime DateTime { get; set; }
        public decimal Macd { get; set; }
        public decimal Signal { get; set; }

        public override string ToString()
        {
            return $"{DateTime:t} {decimal.Round(Macd,2)} {decimal.Round(Signal, 2)}";
        }
    }

    public enum MovingAverageStyle
    {
        SMA,
        EMA
    }

}
