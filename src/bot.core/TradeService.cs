using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using bot.core.Extensions;
using bot.model;

namespace bot.core
{
    public interface ITradeService : IService
    {
        Task Trade();
        Task Trade(IExchangeClient client, string pair);
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
        private readonly IStatusService _statusService;

        public TradeService(ITradeRepository tradeRepository, IDateTime dateTime, IEventRepository eventRepository, Config config,
            IOrderService orderService, IExchangeClient[] exchangeClients, ILogRepository logRepository, IFileService fileService, IStatusService statusService)
        {
            _tradeRepository = tradeRepository;
            _dateTime = dateTime;
            _eventRepository = eventRepository;
            _config = config;
            _orderService = orderService;
            _exchangeClients = exchangeClients;
            _logRepository = logRepository;
            _fileService = fileService;
            _statusService = statusService;
        }

        public async Task Trade()
        {
            foreach (var client in _exchangeClients)
            {
                try
                {
                    await _orderService.CheckOperations(client);
                    await _orderService.CheckOpenOrders(client);

                    foreach (var tradePair in _config.PairPercent)
                    {
                        try
                        {
                            await Trade(client, tradePair.Key);
                        }
                        finally
                        {
                            _fileService.Write(tradePair.Key, "----------------------------------------------------");
                        }
                       
                    }

                }
                catch (Exception e)
                {
                    _fileService.Write("error", $"{DateTime.Now:G} {e.Message}");
                    _fileService.Write("error", e.StackTrace);
                    await _logRepository.Log(client.Platform, "Error", e.StackTrace);
                }
                
            }
        }

        public async Task Trade(IExchangeClient client, string pair)
        {
            var currentStatus = await _statusService.GetCurrentStatus(client.Platform, pair);
            await _eventRepository.UpdateLastEvent(client.Platform, $"{EventConstant.Trade} {pair}", string.Empty);
            var dt = _dateTime.Now.AddHours(-_config.AnalyseLoadHours);
            var trades = (await _tradeRepository.LoadTrades(pair, dt, _dateTime.Now)).ToList();
            var groupedTrades = trades.GroupAll(_config.AnalyseGroupPeriodMinutes, GroupBy.Minute).ToList();
            var groupedTradesSlow = trades.GroupAll(_config.AnalyseMacdGroupPeriodMinutesSlow, GroupBy.Minute).ToList().Cast<IDateCost>().ToList();

            var newStatus = FindStatusFromTrades(groupedTrades.Cast<IDateCost>().ToList(), groupedTradesSlow, pair);
            _fileService.Write(pair, $"Price [buy: {Math.Round(groupedTrades.Where(x => x.PriceBuyAvg != decimal.Zero).OrderBy(x => x.DateTime).Last().PriceBuyAvg, 2)}]" +
                               $" [sell: {Math.Round(groupedTrades.Where(x => x.PriceSellAvg != decimal.Zero).OrderBy(x => x.DateTime).Last().PriceSellAvg, 2)}]");
            if (currentStatus == newStatus || newStatus == TradeStatus.Unknown)
            {
                return;
            }
            switch (newStatus)
            {
                case TradeStatus.Buy:
                    {
                        var lastTrade = groupedTrades.Where(x => x.PriceBuyAvg != decimal.Zero).OrderBy(x => x.DateTime).Last();
                        await _orderService.Buy(client, pair, Math.Round(lastTrade.PriceBuyAvg, 2));
                        await _statusService.SetCurrentStatus(client.Platform, TradeStatus.Buy, pair);
                        break;
                    }
                case TradeStatus.Sell:
                    {
                        var lastTrade = groupedTrades.Where(x => x.PriceSellAvg != decimal.Zero).OrderBy(x => x.DateTime).Last();
                        var sellResult = await _orderService.Sell(client, pair, Math.Round(lastTrade.PriceSellAvg, 2));
                        if (sellResult)
                        {
                            await _statusService.SetCurrentStatus(client.Platform, TradeStatus.Sell, pair);
                        }
                        break;
                    }
            }
        }

        public TradeStatus FindStatusFromTrades(List<IDateCost> groupedTrades, List<IDateCost> groupedTradesSlow, string pair)
        {
            var macd = groupedTrades.Macd(_config.AnalyseMacdSlow, _config.AnalyseMacdFast, _config.AnalyseMacdSignal).MacdAnalysis();
            _fileService.Write(pair, $"MACD_F [status:{(macd.CrossType == CrossType.MacdFalls ? "Sell" : "Buy")}] [since:{(int)(_dateTime.Now - macd.Trade.DateTime).TotalMinutes}]");
            var rsiLastPeak = groupedTrades.RelativeStrengthIndex(_config.AnalyseRsiEmaPeriods)
                .GetPeaks(_config.AnalyseRsiLow, _config.AnalyseRsiHigh).OrderByDescending(x => x.PeakTrade.DateTime)
                .FirstOrDefault();
            if (rsiLastPeak != null)
            {
                _fileService.Write(pair,
                    $"RSI [status:{(rsiLastPeak.PeakType == PeakType.High ? "Sell" : "Buy")}] [since:{(int)(_dateTime.Now - rsiLastPeak.ExitTrade.DateTime).TotalMinutes}]");
            }
            else
            {
                _fileService.Write(pair, "No RSI Peaks found");
            }

            var macdSlow = groupedTradesSlow.Macd(_config.AnalyseMacdSlow, _config.AnalyseMacdFast,
                _config.AnalyseMacdSignal).ToList();

            var macdSlowAnalysis = macdSlow.MacdSlowAnalysis(_config.AnalyseMacdSlowThreshold);
            _fileService.Write(pair, $"MACD_S [macd:{Math.Round(macdSlow.Last().Macd, 2)}] [signal:{Math.Round(macdSlow.Last().Signal, 2)}]");
            _fileService.Write(pair, $"MACD_S [status:{macdSlowAnalysis.ToString()}]");

            var status = AnalysisExtensions.AnalyseIndeces(_config.AnalyseTresholdMinutes, _dateTime.Now, macd, rsiLastPeak, macdSlowAnalysis);
            _fileService.Write(pair, $"Analysis [status:{status.ToString()}]");
            return status;
        }
    }
}
