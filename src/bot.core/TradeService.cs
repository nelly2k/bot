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
        private readonly ILogRepository _logRepository;
        private readonly IFileService _fileService;
        private const string Pair = "ETHUSD";

        public TradeService(ITradeRepository tradeRepository, IDateTime dateTime, IEventRepository eventRepository, Config config,
            IOrderService orderService, IExchangeClient[] exchangeClients, ILogRepository logRepository, IFileService fileService)
        {
            _tradeRepository = tradeRepository;
            _dateTime = dateTime;
            _eventRepository = eventRepository;
            _config = config;
            _orderService = orderService;
            _exchangeClients = exchangeClients;
            _logRepository = logRepository;
            _fileService = fileService;
        }

        public async Task Trade()
        {
            foreach (var client in _exchangeClients)
            {
                try
                {
                    await _orderService.CheckOpenOrders(client);
                    await Trade(client);
                }
                catch (Exception e)
                {
                    _fileService.Write($"{DateTime.Now:G} {e.Message}");
                    _fileService.Write(e.StackTrace);
                    await _logRepository.Log(client.Platform, "Error", e.Message);
                    await _logRepository.Log(client.Platform, "Error Stack Trace", e.StackTrace);
                }
               
            }
        }

        public async Task Trade(IExchangeClient client)
        {
            var currentStatus = await GetCurrentStatus(client.Platform);
            await _eventRepository.UpdateLastEvent(client.Platform, $"{EventConstant.Trade} {Pair}",string.Empty);
            var dt = _dateTime.Now.AddHours(-_config.AnalyseLoadHours);
            var trades = (await _tradeRepository.LoadTrades("XETHZUSD", dt, _dateTime.Now)).ToList();
            var groupedTrades = trades.GroupAll(_config.AnalyseGroupPeriodMinutes, GroupBy.Minute).ToList();
            var groupedTradesSlow = trades.GroupAll(_config.AnalyseMacdGroupPeriodMinutesSlow, GroupBy.Minute).ToList().Cast<IDateCost>().ToList();

            var newStatus = FindStatusFromTrades(groupedTrades.Cast<IDateCost>().ToList(), groupedTradesSlow);
            _fileService.Write($"[buy price: {groupedTrades.Where(x => x.PriceBuyAvg != decimal.Zero).OrderBy(x => x.DateTime).Last().PriceBuyAvg}]" +
                               $" [sell price: {groupedTrades.Where(x => x.PriceSellAvg != decimal.Zero).OrderBy(x => x.DateTime).Last().PriceSellAvg}]");
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

                    break;
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
            var status =  (TradeStatus)Enum.Parse(typeof(TradeStatus), currentStatusStr);
            _fileService.Write($"Get status for [platform:{platform}] [new status:{status}]");
            return status;
        }

        public async Task SetCurrentStatus(string platform,TradeStatus tradeStatus)
        {
            _fileService.Write($"Set status for [platform:{platform}] [new status:{tradeStatus}]");
            await _eventRepository.UpdateLastEvent(platform, $"{EventConstant.StatusUpdate} {Pair}",
                tradeStatus.ToString());
        }

        public TradeStatus FindStatusFromTrades(List<IDateCost> groupedTrades, List<IDateCost> groupedTradesSlow)
        {
            var macd = groupedTrades.Macd(_config.AnalyseMacdSlow, _config.AnalyseMacdFast, _config.AnalyseMacdSignal).MacdAnalysis();
            _fileService.Write($"MACD Fast [cross type:{macd.CrossType.ToString()}] [means: {(macd.CrossType== CrossType.MacdFalls?"Sell": "Buy")}] [at:{macd.Trade.DateTime:G}] [since:{(int)(_dateTime.Now - macd.Trade.DateTime).TotalMinutes}]");
            var rsiLastPeak = groupedTrades.RelativeStrengthIndex(_config.AnalyseRsiEmaPeriods)
                .GetPeaks(_config.AnalyseRsiLow, _config.AnalyseRsiHigh).OrderByDescending(x => x.PeakTrade.DateTime)
                .FirstOrDefault();
            if (rsiLastPeak != null)
            {
                _fileService.Write(
                    $"RSI Last peak [peak type:{rsiLastPeak.PeakType.ToString()}] [means: {(rsiLastPeak.PeakType == PeakType.High ? "Sell" : "Buy")}] [at:{rsiLastPeak.ExitTrade.DateTime:G}] [since:{(int)(_dateTime.Now - rsiLastPeak.ExitTrade.DateTime).TotalMinutes}]");
            }
            else
            {
                _fileService.Write("No RSI Peaks found");
            }
            
            var macdSlow = groupedTradesSlow.Macd(_config.AnalyseMacdSlow, _config.AnalyseMacdFast,
                _config.AnalyseMacdSignal).MacdSlowAnalysis();
            _fileService.Write($"MACD Slow [trade status:{macdSlow.ToString()}]");

            var status= AnalysisExtensions.AnalyseIndeces(_config.AnalyseTresholdMinutes, _dateTime.Now, macd, rsiLastPeak, macdSlow);
            _fileService.Write($"Analysis [trade status:{status.ToString()}]");
            return status;
        }
    }
}
