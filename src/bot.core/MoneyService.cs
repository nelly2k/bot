using System;
using bot.model;

namespace bot.core
{
    public interface IMoneyService:IService
    {
        CurrencyAmountResult Transform(decimal baseCurrencyAmount, decimal currencyPrice, decimal feePercent, FeeSource feeSource = FeeSource.Base);
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


    }
}