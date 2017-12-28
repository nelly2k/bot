using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bot.core.Extensions;
using bot.model;

namespace bot.core
{
    public class TradeService : ITradeService
    {
        private readonly ITradeRepository _tradeRepository;
        private readonly IDateTime _dateTime;
        private readonly IEventRepository _eventRepository;
        private readonly Config _config;
        private readonly IOrderService _orderService;
        private readonly IExchangeClient[] _exchangeClients;
        private readonly IFileService _fileService;
        private readonly IStatusService _statusService;

        public TradeService(ITradeRepository tradeRepository, IDateTime dateTime, IEventRepository eventRepository, Config config,
            IOrderService orderService, IExchangeClient[] exchangeClients, IFileService fileService, IStatusService statusService)
        {
            _tradeRepository = tradeRepository;
            _dateTime = dateTime;
            _eventRepository = eventRepository;
            _config = config;
            _orderService = orderService;
            _exchangeClients = exchangeClients;
            _fileService = fileService;
            _statusService = statusService;
        }

        public async Task Trade()
        {
            foreach (var client in _exchangeClients)
            {
                await CheckOutstanding(client);

                foreach (var tradePair in _config.Pairs.Where(x => x.Value.ShouldTrade))
                {
                    _fileService.StartSession(tradePair.Key);
                    _fileService.GatherDetails(tradePair.Key, FileSessionNames.Date, _dateTime.Now.ToString("G"));
                    await TradePair(client, tradePair);
                    _fileService.CloseSession(tradePair.Key);
                }
            }
        }

        private async Task TradePair(IExchangeClient client, KeyValuePair<string, PairConfig> tradePair)
        {
            try
            {
                await Trade(client, tradePair.Key);
            }
            catch (Exception ex)
            {
                _fileService.Write(tradePair.Key, ex);
            }
        }

        private async Task CheckOutstanding(IExchangeClient client)
        {
            try
            {
                await _orderService.CheckOperations(client);
                await _orderService.CheckOpenOrders(client);
            }
            catch (Exception e)
            {
                _fileService.Write("general", e);
            }
        }

        public async Task Trade(IExchangeClient client, string pair)
        {
            var currentStatus = await _statusService.GetCurrentStatus(client.Platform, pair);

            _fileService.GatherDetails(pair, FileSessionNames.Status, currentStatus.ToString());

            await _eventRepository.UpdateLastEvent(client.Platform, $"{EventConstant.Trade} {pair}", string.Empty);
            var dt = _dateTime.Now.AddHours(-_config[pair].LoadHours);
            var trades = (await _tradeRepository.LoadTrades(pair, dt, _dateTime.Now)).ToList();
            var groupedTrades = trades.GroupAll(_config[pair].GroupMinutes, GroupBy.Minute).ToList();
            var groupedTradesSlow = trades.GroupAll(_config[pair].GroupForLongMacdMinutes, GroupBy.Minute).ToList()
                .Cast<IDateCost>().ToList();

            var newStatus = FindStatusFromTrades(groupedTrades.Cast<IDateCost>().ToList(), groupedTradesSlow, pair);

            _fileService.GatherDetails(pair, FileSessionNames.PriceSell,
                groupedTrades.Where(x => x.PriceSellAvg != decimal.Zero).OrderBy(x => x.DateTime).Last().PriceSellAvg);
            _fileService.GatherDetails(pair, FileSessionNames.PriceBuy,
                groupedTrades.Where(x => x.PriceBuyAvg != decimal.Zero).OrderBy(x => x.DateTime).Last().PriceBuyAvg);
            _fileService.GatherDetails(pair, FileSessionNames.Volume, groupedTrades.Last().Volume);

            if (currentStatus == newStatus || newStatus == TradeStatus.Unknown)
            {
                return;
            }
            
            switch (newStatus)
            {
                case TradeStatus.Return:
                    await Return(client, pair, groupedTrades);
                    await _statusService.SetCurrentStatus(client.Platform, TradeStatus.Return, pair);
                    break;
                case TradeStatus.Borrow:
                    await Borrow(client, pair, groupedTrades);
                    await _statusService.SetCurrentStatus(client.Platform, TradeStatus.Borrow, pair);
                    break;
                case TradeStatus.Buy:
                    await Buy(client, pair, groupedTrades);
                    await Return(client, pair, groupedTrades);
                    await _statusService.SetCurrentStatus(client.Platform, TradeStatus.Buy, pair);
                    break;
                case TradeStatus.Sell:
                    await Sell(client, pair, groupedTrades);
                    await Borrow(client, pair, groupedTrades);
                    break;
                default:
                    return;
            }
        }

        private async Task Sell(IExchangeClient client, string pair, List<GroupResult> groupedTrades)
        {
            var lastTrade = groupedTrades.Where(x => x.PriceSellAvg != decimal.Zero).OrderBy(x => x.DateTime).Last();
            var sellResult =
                await _orderService.Sell(client, pair, Math.Round(lastTrade.PriceSellAvg, _config[pair].PriceFormat));
            if (sellResult)
            {
                await _statusService.SetCurrentStatus(client.Platform, TradeStatus.Sell, pair);
            }
        }

        private async Task Return(IExchangeClient client, string pair, List<GroupResult> groupedTrades)
        {
            var lastTrade = groupedTrades.Where(x => x.PriceBuyAvg != decimal.Zero).OrderBy(x => x.DateTime).Last();
            var result = await _orderService.Return(client, pair, Math.Round(lastTrade.PriceBuyAvg, _config[pair].PriceFormat));
            if (result)
            {
                await _statusService.SetCurrentStatus(client.Platform, TradeStatus.Sell, pair);
            }
        }

        private async Task Borrow(IExchangeClient client, string pair, List<GroupResult> groupedTrades)
        {
            var lastTrade = groupedTrades.Where(x => x.PriceSellAvg != decimal.Zero).OrderBy(x => x.DateTime).Last();
            await _orderService.Borrow(client, pair, Math.Round(lastTrade.PriceSellAvg, _config[pair].PriceFormat));
        }


        private async Task Buy(IExchangeClient client, string pair, List<GroupResult> groupedTrades)
        {
            var lastTrade = groupedTrades.Where(x => x.PriceBuyAvg != decimal.Zero).OrderBy(x => x.DateTime).Last();
            await _orderService.Buy(client, pair, Math.Round(lastTrade.PriceBuyAvg, _config[pair].PriceFormat));
            
        }


        public TradeStatus FindStatusFromTrades(List<IDateCost> groupedTrades, List<IDateCost> groupedTradesSlow, string pair)
        {
            var macd = groupedTrades.Macd(_config[pair].MacdSlow, _config[pair].MacdFast, _config[pair].MacdSignal);
            var macdAnalysis = macd.MacdAnalysis();

            _fileService.GatherDetails(pair, FileSessionNames.MACD_Fast_Value, macdAnalysis.Trade.Price);

            if (macdAnalysis.Trade is BaseTrade macdTrade)
            {
                _fileService.GatherDetails(pair, FileSessionNames.MACD_Fast_Signal, macdTrade.Volume);
            }

            _fileService.GatherDetails(pair, FileSessionNames.MACD_Fast_Minutes, (_dateTime.Now - macdAnalysis.Trade.DateTime).TotalMinutes);
            _fileService.GatherDetails(pair, FileSessionNames.MACD_Fast_Decision, macdAnalysis.CrossType == CrossType.MacdFalls ? "sell" : "buy");


            var rsiLastPeak = groupedTrades.RelativeStrengthIndex(_config[pair].RsiEmaPeriods)
                .GetPeaks(_config[pair].RsiLow, _config[pair].RsiHigh).OrderByDescending(x => x.PeakTrade.DateTime)
                .FirstOrDefault();
            if (rsiLastPeak != null)
            {
                _fileService.GatherDetails(pair, FileSessionNames.RSI_Peak, rsiLastPeak.PeakTrade.Price);
                _fileService.GatherDetails(pair, FileSessionNames.RSI_Analysis_Minutes, (_dateTime.Now - rsiLastPeak.ExitTrade.DateTime).TotalMinutes);
                _fileService.GatherDetails(pair, FileSessionNames.RSI_Decision, rsiLastPeak.PeakType == PeakType.High ? "sell" : "buy");
            }
          
            var macdSlow = groupedTradesSlow.Macd(_config[pair].MacdSlow, _config[pair].MacdFast, _config[pair].MacdSignal).ToList();

            var macdSlowAnalysis = macdSlow.MacdSlowAnalysis();

            _fileService.GatherDetails(pair, FileSessionNames.MACD_Slow_Analysis, macdSlowAnalysis.ToString());
            _fileService.GatherDetails(pair, FileSessionNames.MACD_Slow_Analysis_Value, macdSlow.Last().Macd);
            _fileService.GatherDetails(pair, FileSessionNames.MACD_Slow_Analysis_Signal, macdSlow.Last().Signal);


            var status = AnalysisExtensions.AnalyseIndeces(_config[pair].ThresholdMinutes, _dateTime.Now, macdAnalysis, rsiLastPeak, macdSlowAnalysis);

            _fileService.GatherDetails(pair, FileSessionNames.Analysis, status.ToString());
            return status;
        }
    }
}
