using System.Collections.Generic;

namespace bot.kraken.Model
{
    public class SinceResponse<TResult>
    {
        public List<TResult> Results { get; set; }
        public string LastId { get; set; }
        
    }
}