using NUnit.Framework;

namespace bot.core.tests
{
    public class WhenCalcCurrencyAmount
    {

        [Test]
        public void USDToETH_Sixty_Fee()
        {
            var core = new CoreService();
            var result = core.Transform(60m, 287m, 0.26m);
            Assert.That(result.Fee, Is.EqualTo(0.16).Within(0.001));
        }

        [Test]
        public void USDToETH_Sixty_TargetCurrencyAmount()
        {
            var core = new CoreService();
            var result = core.Transform(60m, 287m, 0.26m);
            Assert.That(result.TargetCurrencyAmount, Is.EqualTo(0.2).Within(0.01));
        }

        [Test]
        public void USDToETH_Sixty_RestBaseCurrency()
        {
            var core = new CoreService();
            var result = core.Transform(60m, 287m, 0.26m);
            Assert.That(result.BaseCurrencyRest, Is.EqualTo(2.44).Within(0.01));
        }

        [Test]
        public void ETHToUSD_Sixty_Fee()
        {
            var core = new CoreService();
            var result = core.Transform(0.2m, 287m, 0.16m, FeeSource.Target);
            Assert.That(result.Fee, Is.EqualTo(0.09).Within(0.001));
        }

        [Test]
        public void ETHToUSD_Sixty_RestBaseCurrency()
        {
            var core = new CoreService();
            var result = core.Transform(0.2m, 287m, 0.16m, FeeSource.Target);
            Assert.That(result.BaseCurrencyRest, Is.EqualTo(decimal.Zero));
        }

        [Test]
        public void ETHToUSD_Sixty_TargetCurrencyAmount()
        {
            var core = new CoreService();
            var result = core.Transform(0.2m, 287m, 0.16m, FeeSource.Target);
            Assert.That(result.TargetCurrencyAmount, Is.EqualTo(57.31m).Within(0.01));
        }
    }
}
