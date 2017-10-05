namespace bot.kraken
{
    public interface IKrakenCredentials
    {
        string Key { get; set; }
        string Secret { get; set; }
    }

    public class KrakenCredentials : IKrakenCredentials
    {
        public string Key { get; set; }
        public string Secret { get; set; }
    }
}
