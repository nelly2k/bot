using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bot.core.Extensions;
using bot.model;

namespace bot.core
{
    public interface ITradeService:IService
    {
        Task Trade();
        Task Trade(IExchangeClient client);
        Task<TradeStatus> GetCurrentStatus(string platform);
    }

    public class TradeService : ITradeService
    {
        private readonly ITradeRepository _tradeRepository;
        private readonly IDateTime _dateTime;
        private readonly IEventRepository _eventRepository;
        private readonly Config _config;
        private readonly IOrderService _orderService;
        private readonly IExchangeClient[] _exchangeClients;
        private const string Pair = "XETHZUSD";

        public TradeService(ITradeRepository tradeRepository, IDateTime dateTime, IEventRepository eventRepository, Config config,
            IOrderService orderService, IExchangeClient[] exchangeClients)
        {
            _tradeRepository = tradeRepository;
            _dateTime = dateTime;
            _eventRepository = eventRepository;
            _config = config;
            _orderService = orderService;
            _exchangeClients = exchangeClients;
        }

        public async Task Trade()
        {
            foreach (var client in _exchangeClients)
            {
                await Trade(client);
            }
        }

        public async Task Trade(IExchangeClient client)
        {
            var currentStatus = await GetCurrentStatus(client.Platform);

            var dt = _dateTime.Now.AddHours(-_config.AnalyseLoadHours);
            var trades = (await _tradeRepository.LoadTrades(Pair, dt, _dateTime.Now)).ToList();
            var groupedTrades = trades.GroupAll(_config.AnalyseGroupPeriodMinutes, GroupBy.Minute).ToList();
            var groupedTradesSlow = trades.GroupAll(_config.AnalyseMacdGroupPeriodMinutesSlow, GroupBy.Minute).ToList().Cast<IDateCost>().ToList();

            var newStatus = FindStatusFromTrades(groupedTrades.Cast<IDateCost>().ToList(), groupedTradesSlow);
            
            if (currentStatus == newStatus || newStatus == TradeStatus.Unknown)
            {
                return;
            }
            switch (newStatus)
            {
                case TradeStatus.Buy:
                {
                    var lastTrade = groupedTrades.Where(x => x.PriceBuyAvg != decimal.Zero).OrderBy(x => x.DateTime).Last();
                    await _orderService.Buy(client, Pair, lastTrade.PriceBuyAvg);
                    
                    return;
                }
                case TradeStatus.Sell:
                {
                    var lastTrade = groupedTrades.Where(x => x.PriceSellAvg != decimal.Zero).OrderBy(x => x.DateTime).Last();
                    await _orderService.Sell(client, Pair, lastTrade.PriceSellAvg);
                    break;
                }
            }
            await SetCurrentStatus(client.Platform, newStatus);
        }

        public async Task<TradeStatus> GetCurrentStatus(string platform)
        {
            var currentStatusStr = await _eventRepository.GetLastEventValue(platform, $"{EventConstant.StatusUpdate} {Pair}");
            return (TradeStatus)Enum.Parse(typeof(TradeStatus), currentStatusStr);
        }

        public async Task SetCurrentStatus(string platform,TradeStatus tradeStatus)
        {
            await _eventRepository.UpdateLastEvent(platform, $"{EventConstant.StatusUpdate} {Pair}",
                tradeStatus.ToString());
        }

        public TradeStatus FindStatusFromTrades(List<IDateCost> groupedTrades, List<IDateCost> groupedTradesSlow)
        {
            var macd = groupedTrades.Macd(_config.AnalyseMacdSlow, _config.AnalyseMacdFast, _config.AnalyseMacdSignal).MacdAnalysis();
            var rsiLastPeak = groupedTrades.RelativeStrengthIndex(_config.AnalyseRsiEmaPeriods)
                .GetPeaks(_config.AnalyseRsiLow, _config.AnalyseRsiHigh).OrderByDescending(x => x.PeakTrade.DateTime)
                .FirstOrDefault();

            var macdSlow = groupedTradesSlow.Macd(_config.AnalyseMacdSlow, _config.AnalyseMacdFast,
                _config.AnalyseMacdSignal).MacdSlowAnalysis();

            return AnalysisExtensions.AnalyseIndeces(_config.AnalyseTresholdMinutes, _dateTime.Now, macd, rsiLastPeak, macdSlow);
        }
    }
}
