﻿using System;
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
        TradeStatus FindStatusFromTrades(List<IDateCost> groupedTrades);
        Task<TradeStatus> GetCurrentStatus();
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
        private const string Platform = "kraken";

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
            var currentStatus = await GetCurrentStatus();

            var dt = _dateTime.Now.AddHours(-_config.AnalyseLoadHours);
            var trades = await _tradeRepository.LoadTrades(Pair, dt, _dateTime.Now);
            var groupedTrades = trades.GroupAll(_config.AnalyseGroupPeriodMinutes, GroupBy.Minute).ToList();
            var newStatus = FindStatusFromTrades(groupedTrades.Cast<IDateCost>().ToList());
            if (currentStatus == newStatus || newStatus == TradeStatus.Unknown)
            {
                return;
            }
            //TODO where is set status?
            //Sell need to be implemented in simulator
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
        }

        public async Task<TradeStatus> GetCurrentStatus()
        {
            var currentStatusStr = await _eventRepository.GetLastEventValue(Platform, $"{EventConstant.StatusUpdate} {Pair}");
            return (TradeStatus)Enum.Parse(typeof(TradeStatus), currentStatusStr);
        }

        public TradeStatus FindStatusFromTrades(List<IDateCost> groupedTrades)
        {
            var macd = groupedTrades.Macd(_config.AnalyseMacdSlow, _config.AnalyseMacdFast, _config.AnalyseMacdSignal).MacdAnalysis();
            var rsiLastPeak = groupedTrades.RelativeStrengthIndex(_config.AnalyseRsiEmaPeriods)
                .GetPeaks(_config.AnalyseRsiLow, _config.AnalyseRsiHigh).OrderByDescending(x => x.PeakTrade.DateTime)
                .FirstOrDefault();
            return AnalysisExtensions.AnalyseIndeces(_config.AnalyseTresholdMinutes, _dateTime.Now, macd, rsiLastPeak);
        }


    }



}
