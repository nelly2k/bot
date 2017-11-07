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
            var id = await repo.Add("test", "order");

            Assert.That(id, Is.GreaterThan(0));

            var incomplete = await repo.GetIncomplete("test", "order");

            Assert.That(incomplete.Count, Is.EqualTo(1));

            await repo.Complete(id);

            var incomplete1 = await repo.GetIncomplete("test", "order");
            Assert.That(incomplete1.Count, Is.EqualTo(0));
        }
    }
}