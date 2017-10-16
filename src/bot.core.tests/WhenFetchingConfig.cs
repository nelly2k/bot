using System.Threading.Tasks;
using NUnit.Framework;

namespace bot.core.tests
{
    public class WhenFetchingConfig
    {
        [Test]
        public async Task FetchConfig()
        {
            var repo = new ConfigRepository();
            var config = await repo.Get();

            Assert.That(config.Key, Is.Not.Null);

        }
    }
}
