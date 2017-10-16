using System.Collections.Generic;
using System.Threading.Tasks;

namespace bot.model
{
    public interface IExchangeClient
    {
        string Platform { get; }
        Task<SinceResponse<ITrade>> GetTrades(string lastId = null, params string[] pairs);

        Task<List<Order>> GetOrders(params string[] refs);

        Task<decimal> GetBaseCurrencyBalance();

        Task<List<string>> AddOrder(OrderType orderType, decimal volume, string pair = "ETHUSD");
    }
}