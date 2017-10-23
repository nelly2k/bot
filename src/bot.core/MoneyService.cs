using System;
using bot.model;

namespace bot.core
{
    public interface IMoneyService:IService
    {
        CurrencyAmountResult Transform(decimal baseCurrencyAmount, decimal currencyPrice, decimal feePercent, FeeSource feeSource = FeeSource.Base);
        decimal FeeToPay(decimal volume, decimal price, decimal feePercent);
    }

    public class MoneyService : IMoneyService
    {
        public CurrencyAmountResult Transform(decimal baseCurrencyAmount, decimal currencyPrice, decimal feePercent, FeeSource feeSource = FeeSource.Base)
        {
            var result = new CurrencyAmountResult();
            switch (feeSource)
            {
                case FeeSource.Base:
                    result.Fee = Math.Round(baseCurrencyAmount * feePercent / 100, 2);
                    result.TargetCurrencyAmount = Math.Floor((baseCurrencyAmount - result.Fee) / currencyPrice * 100) / 100m;
                    result.BaseCurrencyRest = baseCurrencyAmount - (result.TargetCurrencyAmount * currencyPrice + result.Fee);
                    break;
                case FeeSource.Target:
                    var baseUsd = currencyPrice * baseCurrencyAmount;
                    result.Fee = Math.Round(baseUsd * feePercent / 100, 2);
                    result.BaseCurrencyRest = decimal.Zero;
                    result.TargetCurrencyAmount = baseUsd - result.Fee;
                    break;
            }

            return result;
        }
        
        public decimal FeeToPay(decimal volume, decimal price, decimal feePercent)
        {
            return Math.Round(volume * price * feePercent / 100,2);
        }
    }

    public class CurrencyAmountResult
    {
        public decimal Fee { get; set; }
        public decimal TargetCurrencyAmount { get; set; }
        public decimal BaseCurrencyRest { get; set; }
        public TradeStatus TradeStatus { get; set; }
    }

    public enum FeeSource
    {
        Base,
        Target
    }
}