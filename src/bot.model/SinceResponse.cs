using System.Collections.Generic;

namespace bot.model
{
    public class SinceResponse<TResult>
    {
        public List<TResult> Results { get; set; }
        public string LastId { get; set; }
        
    }
}