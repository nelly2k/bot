using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace bot.core.tests
{
    public class WhenQueryingBalanceRepository
    {
        [Test]
        public async Task GetUpdateRemove()
        {
            var balanceRepository = new BalanceRepository();
            var notSoldRepository = new NotSoldRepository();
            var platform = "testPlatform";
            var pair = "testPair";
            await balanceRepository.Add(platform, pair, 0.25m, 222.56m, false);

            var get = await balanceRepository.Get(platform, pair);
            Assert.That(get.Count, Is.EqualTo(1));

            await notSoldRepository.SetNotSold(platform, pair, false);

            var one = (await balanceRepository.Get(platform, pair)).First();
            Assert.That(one.NotSoldtDate,Is.Not.Null);
            Assert.That(one.NotSold,Is.EqualTo(1));

            Assert.That(one.Volume, Is.EqualTo(0.25m).Within(0.01));
            
            await notSoldRepository.SetNotSold(platform, pair, false);
            one = (await balanceRepository.Get(platform, pair)).First();
            Assert.That(one.NotSold, Is.EqualTo(2));

            await balanceRepository.Remove(platform, pair, false);

            get = await balanceRepository.Get(platform, pair);
            Assert.That(get.Count, Is.EqualTo(0));
        }

    }
}
