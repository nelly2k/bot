using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface IConfigRepository : IService
    {
        Task<Config> Get(string platform = "kraken");
        Task CleanUp(string platform);
        Task Deploy(string platform, params string[] pairs);
    }
}