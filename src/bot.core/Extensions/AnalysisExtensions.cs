﻿using System;
using System.Collections.Generic;
using System.Linq;
using bot.model;

namespace bot.core.Extensions
{
    public static class AnalysisExtensions
    {

        public static TradeStatus AnalyseIndeces(int treshold, DateTime now, MacdAnalysisResult macdAnalysisResult, PeakAnalysisResult lastRsiPeak, TradeStatus macdSlow)
        {
            if (lastRsiPeak == null || macdAnalysisResult.CrossType == null)
            {
                return TradeStatus.Unknown;
            }

            var minutesSincePeak = (now - lastRsiPeak.ExitTrade.DateTime).TotalMinutes;
            var minutesSinceCross = (now - macdAnalysisResult.Trade.DateTime).TotalMinutes;

            if (minutesSinceCross > treshold || minutesSincePeak > treshold)
            {
                return TradeStatus.Unknown;
            }

            if (macdAnalysisResult.CrossType == CrossType.MacdRises && lastRsiPeak.PeakType == PeakType.Low)
            {
               // return macdSlow == TradeStatus.Buy ? TradeStatus.Buy : TradeStatus.Return;
                return TradeStatus.Buy;
            }

            if (macdAnalysisResult.CrossType == CrossType.MacdFalls && lastRsiPeak.PeakType == PeakType.High)
            {
                //return macdSlow == TradeStatus.Sell ? TradeStatus.Sell : TradeStatus.Borrow;
                return TradeStatus.Sell;
            }

            return TradeStatus.Unknown;
        }

        public static TradeStatus MacdSlowAnalysis(this IEnumerable<MacdResultItem> macd, decimal threshold = 0.0m)
        {
            var lastThree = macd.OrderByDescending(x => x.DateTime).Take(3).ToArray();
            var last = lastThree.First();

            if (last.Macd >= last.Signal || Math.Abs(last.Macd - last.Signal) <= threshold)
            {
                return TradeStatus.Buy;
            }

            return TradeStatus.Sell;
        }

        public static MacdAnalysisResult MacdAnalysis(this IEnumerable<MacdResultItem> macd)
        {
            var list = macd.OrderByDescending(x => x.DateTime).ToList();
            CrossType? currentType = null;
            MacdResultItem previous = null;
            var result = new MacdAnalysisResult();
            foreach (var item in list)
            {
                if (currentType == null)
                {
                    currentType = item.Macd < item.Signal ? CrossType.MacdFalls : CrossType.MacdRises;
                }

                if (currentType == CrossType.MacdFalls && item.Macd > item.Signal && previous != null)
                {
                    result.CrossType = CrossType.MacdFalls;
                    result.Trade = new BaseTrade
                    {
                        DateTime = previous.DateTime,
                        Price = previous.Macd,
                        Volume = previous.Signal
                    };
                    break;
                }

                if (currentType == CrossType.MacdRises && item.Macd < item.Signal && previous != null)
                {
                    result.CrossType = CrossType.MacdRises;
                    result.Trade = new BaseTrade
                    {
                        DateTime = previous.DateTime,
                        Price = previous.Macd,
                        Volume = previous.Signal
                    };
                    break;
                }
                previous = item;
            }
            return result;
        }

        public static IEnumerable<PeakAnalysisResult> GetPeaks(this IEnumerable<IDateCost> list, int lowBorder,
            int highBorder)
        {
            var result = new List<PeakAnalysisResult>();

            var arr = list.OrderByDescending(x => x.DateTime).ToArray();

            IDateCost lowPeak = null;
            IDateCost lowExit = null;

            IDateCost highPeak = null;
            IDateCost highExit = null;

            foreach (var trade in arr)
            {
                if (trade.Price < lowBorder)
                {
                    if (lowPeak == null || lowPeak.Price > trade.Price)
                    {
                        lowPeak = trade;
                    }

                    if (lowExit == null)
                    {
                        lowExit = trade;
                    }
                }

                if (trade.Price >= lowBorder && lowPeak != null)
                {
                    result.Add(new PeakAnalysisResult
                    {
                        PeakType = PeakType.Low,
                        PeakTrade = lowPeak,
                        ExitTrade = lowExit
                    });
                    lowPeak = null;
                    lowExit = null;
                }

                if (trade.Price > highBorder)
                {
                    if (highPeak == null || highPeak.Price < trade.Price)
                    {
                        highPeak = trade;
                    }

                    if (highExit == null)
                    {
                        highExit = trade;
                    }
                }

                if (trade.Price <= highBorder && highPeak != null)
                {
                    result.Add(new PeakAnalysisResult
                    {
                        PeakType = PeakType.High,
                        PeakTrade = highPeak,
                        ExitTrade = highExit
                    });
                    highPeak = null;
                    highExit = null;
                }
            }

            return result;
        }

    }

    public class PeakAnalysisResult
    {
        public IDateCost PeakTrade { get; set; }
        public IDateCost ExitTrade { get; set; }
        public PeakType PeakType { get; set; }
    }

    public enum PeakType
    {
        Low, High
    }

    public class MacdAnalysisResult
    {
        public CrossType? CrossType { get; set; }
        public IDateCost Trade { get; set; }
    }

    public enum CrossType
    {
        MacdFalls, MacdRises
    }

    public enum PriceStatus
    {
        GoingFall,
        GoinRaise,
        Unknown
    }
}