using System;

namespace bot.kraken.Model
{
    public class ServerTime
    {
        public string Unixtime { get; set; }
        public DateTime Rfc1123 { get; set; }
    }
}