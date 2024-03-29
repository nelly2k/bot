﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace bot.model
{
    public interface IExchangeClient
    {
        string Platform { get; }
        Task<SinceResponse<ITrade>> GetTrades(string lastId = null, params string[] pairs);

        Task<List<Order>> GetOrders(params string[] refs);
        Task<List<Order>> GetOpenOrders(int? userref = null);

        Task<decimal> GetBaseCurrencyBalance();
        Task<List<string>> GetOrdersIds(int userref);
        Task<List<string>> Buy(decimal volume, string pair, decimal? price = null, int? operationId = null, bool isReturn = false);
        Task<List<string>> Sell(decimal volume, string pair, decimal? price = null, int? operationId = null, bool isBorrow = false);

        Task<decimal> GetAvailableMargin(string asset);
    }
}