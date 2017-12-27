namespace bot.kraken
{
    public enum TradeBalanceType
    {
        /// <summary>
        /// equivalent balance (combined balance of all currencies)
        /// </summary>
        EquivalentBalance,

        /// <summary>
        /// trade balance (combined balance of all equity currencies)
        /// </summary>
        TradeBalance,

        /// <summary>
        /// margin amount of open positions
        /// </summary>
        Margin,

        /// <summary>
        /// unrealized net profit/loss of open positions
        /// </summary>
        UnrealizedProfitLoss,

        /// <summary>
        /// cost basis of open positions
        /// </summary>
        Cost,

        /// <summary>
        /// current floating valuation of open positions
        /// </summary>
        Valuation,

        /// <summary>
        /// equity = trade balance + unrealized net profit/loss
        /// </summary>
        Equity,

        /// <summary>
        /// free margin = equity - initial margin (maximum margin available to open new positions)
        /// </summary>
        FreeMargin,

        /// <summary>
        /// margin level = (equity / initial margin) * 100
        /// </summary>
        MarginLevel
    }
}