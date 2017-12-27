using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using bot.model;

namespace bot.kraken
{
    public interface IKrakenRepository:IService
    {
        Task<TResult> CallPublic<TResult>(string url, Dictionary<string, string> paramPairs = null);
        Task<TResult> CallPrivate<TResult>(string url, Dictionary<string, string> paramPairs = null);
        DateTime UnixTimeStampToDateTime(double unixTimeStamp);
    }
}