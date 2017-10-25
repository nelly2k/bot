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
            await balanceRepository.Add(platform, pair, 0.25m, 222.56m);

            var get = await balanceRepository.Get(platform, pair);
            Assert.That(get.Count, Is.EqualTo(1));

            await notSoldRepository.SetNotSold(platform, pair);

            var one = (await balanceRepository.Get(platform, pair)).First();
            Assert.That(one.NotSoldtDate,Is.Not.Null);
            Assert.That(one.NotSold,Is.EqualTo(1));
            
            await notSoldRepository.SetNotSold(platform, pair);
            one = (await balanceRepository.Get(platform, pair)).First();
            Assert.That(one.NotSold, Is.EqualTo(2));

            await balanceRepository.Remove(platform, pair);

            get = await balanceRepository.Get(platform, pair);
            Assert.That(get.Count, Is.EqualTo(0));
        }

    }
}
