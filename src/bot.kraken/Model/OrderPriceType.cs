namespace bot.kraken.Model
{
    public enum OrderPriceType
    {
        market = 1,
        limit = 2,// (price = limit price)
        stop_loss = 3, // (price = stop loss price)
        take_profit = 4, // (price = take profit price)
        stop_loss_profit = 5, // (price = stop loss price, price2 = take profit price)
        stop_loss_profit_limit = 6, // (price = stop loss price, price2 = take profit price)
        stop_loss_limit = 7,// (price = stop loss trigger price, price2 = triggered limit price)
        take_profit_limit = 8, // (price = take profit trigger price, price2 = triggered limit price)
        trailing_stop = 9, //(price = trailing stop offset)
        trailing_stop_limit = 10,// (price = trailing stop offset, price2 = triggered limit offset)
        stop_loss_and_limit = 11,// (price = stop loss price, price2 = limit price)
    }
}