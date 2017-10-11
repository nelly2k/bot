using System.Threading.Tasks;

namespace bot.model
{
    public interface IExchangeClient
    {
        string Platform { get; }
        Task<SinceResponse<ITrade>> GetTrades(string lastId = null, params string[] pairs);
    }
}