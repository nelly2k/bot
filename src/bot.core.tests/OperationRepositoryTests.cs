using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace bot.core.tests
{
    public class OperationRepositoryTests
    {
        [Test]
        public async Task AddCompleteGet()
        {
            var repo = new OperationRepository();
            var id = await repo.Add("test", "order", "ETHUSD", "sell");

            Assert.That(id, Is.GreaterThan(0));

            var incomplete = await repo.GetIncomplete("test", "order");

            var one = incomplete.Single();
            Assert.That(one.Misc, Is.EqualTo("sell"));
            Assert.That(one.Id, Is.EqualTo(id));
            Assert.That(one.Pair, Is.EqualTo("ETHUSD"));

            await repo.Complete(id);

            var incomplete1 = await repo.GetIncomplete("test", "order");
            Assert.That(incomplete1.Count, Is.EqualTo(0));

            
        }
    }
}