namespace bot.kraken.Model
{
    public class KrakenResponse<TResult>
    {
        public string[] Error { get; set; }
        public TResult Result { get; set; }
    }
}