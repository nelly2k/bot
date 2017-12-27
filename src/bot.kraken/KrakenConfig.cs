using System.Collections.Generic;
using bot.model;

namespace bot.kraken
{
    public interface IKrakenConfig: IExchangeConfig, IService
    {
        
    }

    public class KrakenConfig: IKrakenConfig
    {
        public static string LongLaverage = "long laverage";
        public static string ShortLaverage = "short laverage";

        public Dictionary<string, object> PairVariables => new Dictionary<string, object>
        {
            {LongLaverage, null},
            {ShortLaverage, 5 }
        };

        public string Platform => "kraken";
    }
}