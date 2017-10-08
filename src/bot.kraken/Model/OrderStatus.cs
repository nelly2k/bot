namespace bot.kraken.Model
{
    public enum OrderStatus
    {
        pending = 1, // order pending book entry
        open = 2, // open order
        closed = 3, //cosed order
        canceled = 4, // order canceled
        expired = 5 // order expired
    }
}