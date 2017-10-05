using System;
using System.Collections.Generic;
using System.Linq;
using bot.model;

namespace bot.core.Extensions
{
    public static class GroupingExtensions
    {
        public static IEnumerable<ITradePrice> GroupPrice(this IEnumerable<ITradePrice> raw, int groupNum, GroupBy by, Operation operation)
        {
            return raw.GroupBy(x => GroupBy(x, groupNum, by))
                .Select(g => new BaseTrade { DateTime = g.Key, Price = CalcPrice(g, operation), Volume = g.Sum(x => x.Volume) })
                .ToList();
        }

        public static IEnumerable<GroupResult> GroupAll(this IEnumerable<ITradePrice> raw, int groupNum, GroupBy by)
        {
            return raw.GroupBy(x => GroupBy(x, groupNum, by))
                .Select(g => new GroupResult
                {
                    DateTime = g.Key,
                    Price = CalcPrice(g, Operation.Average),
                    Volume = g.Sum(x => x.Volume),
                    PriceMin = CalcPrice(g, Operation.Min),
                    PriceMax = CalcPrice(g, Operation.Max),
                    PriceBuyAvg = g.Where(x => x.TradeType == TradeType.Buy).Select(x=>x.Price).DefaultIfEmpty(0).Average(),
                    PriceSellAvg = g.Where(x => x.TradeType == TradeType.Sell).Select(x => x.Price).DefaultIfEmpty(0).Average(),
                })
                .ToList();
        }

        

        private static DateTime GroupBy(ITradePrice x, int groupNum, GroupBy by)
        {
            var stamp = x.DateTime;
            switch (by)
            {
                case Extensions.GroupBy.Day:
                    var day = stamp.AddDays(-(stamp.Day % groupNum));
                    stamp = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0);

                    break;
                case Extensions.GroupBy.Hour:
                    var hours = stamp.AddHours(-(stamp.Hour % groupNum));
                    stamp = new DateTime(hours.Year, hours.Month, hours.Day, hours.Hour, 0, 0);
                    break;
                case Extensions.GroupBy.Minute:
                    var min = stamp.AddMinutes(-(stamp.Minute % groupNum));
                    stamp = new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute, 0);
                    break;
            }
            return stamp;
        }


        private static decimal CalcPrice(IEnumerable<ITradePrice> g, Operation operation)
        {
            switch (operation)
            {
                case Operation.Sum:
                    return g.Sum(s => s.Price);
                case Operation.Average:
                    return g.Average(s => s.Price);
                case Operation.Min:
                    return g.Min(s => s.Price);
                case Operation.Max:
                    return g.Max(s => s.Price);
                case Operation.Std:
                    return (decimal)Math.Sqrt((double)(g.Average(x => x.Price * x.Price) - (decimal)Math.Pow((double)g.Average(x => x.Price), 2)));
            }
            return g.Sum(s => s.Price);
        }
    }

    public class GroupResult : ITradePrice
    {
        public DateTime DateTime { get; set; }
        public decimal Volume { get; set; }
        public TradeType TradeType { get; set; }
        public decimal PriceMin { get; set; }
        public decimal PriceMax { get; set; }
        public decimal Price { get; set; }

        public decimal PriceSellAvg { get; set; }
        public decimal PriceBuyAvg { get; set; }

    }
}