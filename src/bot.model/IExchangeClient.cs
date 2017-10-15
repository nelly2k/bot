using System.Collections.Generic;
using System.Threading.Tasks;

namespace bot.model
{
    public interface IExchangeClient
    {
        string Platform { get; }
        Task<SinceResponse<ITrade>> GetTrades(string lastId = null, params string[] pairs);

        Task<Dictionary<string, OrderStatus>> GetOrderStatus(params string[] refs);
    }
}