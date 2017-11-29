using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace bot.core.tests
{
    
    public class ConfigRepositoryTests
    {
        [Test]
        public async Task Deploy()
        {
            var repo = new ConfigRepository();
            await repo.Deploy("kraken", "ETHUSD");
        }


        [Test]
        public async Task Get()
        {
            var repo = new ConfigRepository();
            var result = await repo.Get("kraken");
        }
    }
}
