﻿namespace bot.kraken
{
    public class KrakenResponse<TResult>
    {
        public string[] Error { get; set; }
        public TResult Result { get; set; }
    }

    public class KrakenContiniusResponse<TResult>
    {
        public string[] Error { get; set; }
        public TResult Result { get; set; }
    }
}