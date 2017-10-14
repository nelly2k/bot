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
            var repo = new BalanceRepository();
            var platform = "testPlatform";
            var pair = "testPair";
            await repo.Add(platform, pair, 0.25m, 222.56m);

            var get = await repo.Get(platform, pair);
            Assert.That(get.Count, Is.EqualTo(1));

            await repo.SetNotSold(platform, pair);

            var one = (await repo.Get(platform, pair)).First();
            Assert.That(one.NotSoldtDate,Is.Not.Null);
            Assert.That(one.NotSold,Is.EqualTo(1));
            
            await repo.SetNotSold(platform, pair);
            one = (await repo.Get(platform, pair)).First();
            Assert.That(one.NotSold, Is.EqualTo(2));

            await repo.Remove(platform, pair);

            get = await repo.Get(platform, pair);
            Assert.That(get.Count, Is.EqualTo(0));
        }

    }
}
