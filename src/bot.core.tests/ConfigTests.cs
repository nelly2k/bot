using System.Threading.Tasks;
using NUnit.Framework;

namespace bot.core.tests
{
    public class ConfigTests
    {

        [Test]
        public async Task DeployData()
        {
            var repo = new ConfigRepository();
            await repo.Deploy("test", "ETHUSD", "XBTUSD");
        }
    }
}
