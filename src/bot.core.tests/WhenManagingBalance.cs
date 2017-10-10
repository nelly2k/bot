using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace bot.core.tests
{
    public class WhenManagingBalance
    {
        private BalanceService _bs;

        [SetUp]
        public void Setup()
        {
            _bs = new BalanceService();
        }

        [Test]
        public void BuyGetBalace()
        {
            _bs.Buy(287, 0.04m);
            _bs.Buy(287, 0.05m);
            var vol = _bs.GetVolumeToSell(290);
            Assert.That(vol, Is.EqualTo(0.09m).Within(0.001));
        }
        [Test]
        public void BuyGetBalace2()
        {
            _bs.Buy(287, 0.04m);
            _bs.Buy(289, 0.05m);
            var vol = _bs.GetVolumeToSell(290);
            Assert.That(vol, Is.EqualTo(0.09m).Within(0.001));
        }

        [Test]
        public void BuyGetBalace25()
        {
            _bs.Buy(287, 0.04m);
            _bs.Buy(300, 0.05m);
            var vol = _bs.GetVolumeToSell(290);
            Assert.That(vol, Is.EqualTo(0.04m).Within(0.001));
        }

        [Test]
        public void BuyGetBalace3()
        {
            _bs.Buy(287, 0.04m);
            _bs.Buy(300, 0.05m);

            Assert.That(_bs.GetVolumeToSell(), Is.EqualTo(0.09m).Within(0.001));
        }

        [Test]
        public void Sell_LessThanOne()
        {
            _bs.Buy(287, 0.4m);
            _bs.Buy(300, 0.5m);
            _bs.Sell(0.3m);

            Assert.That(_bs.GetVolumeToSell(), Is.EqualTo(0.6m).Within(0.001));
        }

        [Test]
        public void Sell_One_Exactly()
        {
            _bs.Buy(287, 0.4m);
            _bs.Buy(300, 0.5m);
            _bs.Sell(0.4m);

            Assert.That(_bs.GetVolumeToSell(), Is.EqualTo(0.5m).Within(0.001));
        }

        [Test]
        public void Sell_One_Greater()
        {
            _bs.Buy(287, 0.4m);
            _bs.Buy(300, 0.5m);
            _bs.Sell(0.5m);

            Assert.That(_bs.GetVolumeToSell(), Is.EqualTo(0.4m).Within(0.001));
        }

        [Test]
        public void CalcLoss()
        {
            _bs.Buy(287, 0.4m);
            _bs.Buy(300, 0.5m);

            _bs.MarkNotSold(286);
            
            //var getLoss = 

            //Assert.That(_bs.GetVolumeToSell(), Is.EqualTo(0.4m).Within(0.001));
        }
    }
}
