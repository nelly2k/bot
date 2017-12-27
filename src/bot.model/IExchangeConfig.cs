using System.Collections.Generic;

namespace bot.model
{
    public interface IExchangeConfig
    {
        string Platform { get; }
        Dictionary<string, object> PairVariables { get; }
    }
}