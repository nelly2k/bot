using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface ITradeService : IService
    {
        Task Trade();
        Task Trade(IExchangeClient client, string pair);
    }
}