using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bot.core.Extensions;
using bot.model;

namespace bot.core
{
    public interface ITradeService
    {
        /// <summary>
        /// 3. load balance
        /// If status buy
        /// Find out how much I can buy
        /// </summary>
        /// <returns></returns>
        Task Trade();
    }

    public class TradeService : ITradeService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IDateTime _dateTime;
        private readonly IEventRepository _eventRepository;
        private readonly IConfigRepository _configRepository;
        private const string Pair = "XETHZUSD";
        private const string Platform = "kraken";

        public TradeService(IDatabaseService databaseService, IDateTime dateTime, IEventRepository eventRepository,
            IConfigRepository configRepository)
        {
            _databaseService = databaseService;
            _dateTime = dateTime;
            _eventRepository = eventRepository;
            _configRepository = configRepository;
        }

        public static TradeStatus TradeStatus = TradeStatus.Unknown;

        /// <summary>
        /// 3. load balance
        /// If status buy
        /// 
        /// Find out how much I can buy
        /// </summary>
        /// <returns></returns>
        public async Task Trade()
        {
            var config = await _configRepository.Get();
            
            var currentStatus = GetCurrentStatus();

            var dt = DateTime.Now.AddHours(-config.AnalyseLoadHours);
            var groupedTrades = await _databaseService.LoadTrades("XETHZUSD", dt, _dateTime.Now);

            var newStatus = FindStatusFromTrades(groupedTrades, config);
        }

        public void HandleStatusChanges(TradeStatus previousStatus, TradeStatus currentStatus, IDateCost dateCost)
        {
            if (currentStatus == TradeStatus.Unknown)
            {
                return;
            }
        }

        public async Task<TradeStatus> GetCurrentStatus()
        {
            var currentStatusStr = await _eventRepository.GetLastEventValue(Platform, $"{EventConstant.StatusUpdate} {Pair}");
            return (TradeStatus)Enum.Parse(typeof(TradeStatus), currentStatusStr);
        }
        public async Task SetStatus(Config config)
        {
            //try
            //{




            //    Console.WriteLine($"{DateTime.Now:F} Price: {grouped.Last().Price:C} Status: {newStatus.ToString()}");
            //    if (newStatus != TradeStatus && newStatus != TradeStatus.Unknown)
            //    {
            //        var price = grouped.First(x => x.DateTime == macd.Trade.DateTime);
            //        TradeStatus = newStatus;
            //        System.Media.SystemSounds.Asterisk.Play();
            //        await _databaseService.Log("kraken", TradeStatus.ToString(),
            //            $"New status: {newStatus} price: {price.Price:C}");

            //    }
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //}
        }

        public TradeStatus FindStatusFromTrades(IEnumerable<ITrade> groupedTrades, Config config)
        {
        
            var grouped = groupedTrades.GroupAll(config.AnalyseGroupPeriodMinutes, GroupBy.Minute).ToList();
            var macd = grouped.Macd(config.AnalyseMacdSlow, config.AnalyseMacdFast, config.AnalyseMacdSignal).MacdAnalysis();
            var rsiLastPeak = grouped.RelativeStrengthIndex(config.AnalyseRsiEmaPeriods).GetPeaks(config.AnalyseRsiLow, config.AnalyseRsiHigh).OrderByDescending(x => x.PeakTrade.DateTime)
                .FirstOrDefault();
           return  AnalysisExtensions.AnalyseIndeces(config.AnalyseTresholdMinutes, DateTime.Now, macd, rsiLastPeak);
          
        }

      
    }
    public class CurrencyAmountResult
    {
        public decimal Fee { get; set; }
        public decimal TargetCurrencyAmount { get; set; }
        public decimal BaseCurrencyRest { get; set; }
        public TradeStatus TradeStatus { get; set; }
    }

    public enum FeeSource
    {
        Base,
        Target
    }
}
