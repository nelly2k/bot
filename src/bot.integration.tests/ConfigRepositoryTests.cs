using System.Collections.Generic;
using System.Threading.Tasks;
using bot.core;
using bot.kraken;
using bot.model;
using NSubstitute;
using NUnit.Framework;

namespace bot.integration.tests
{

    public class ConfigRepositoryTests
    {
        private ConfigRepository _configRepository;

        [SetUp]
        public void Setup()
        {
            
            _configRepository = new ConfigRepository(new List<IExchangeConfig>().ToArray());
        }

        [Test]
        public async Task Deploy()
        {
            await _configRepository.Deploy("kraken");
        }

        [Test]
        public async Task Deploy2()
        {
            const string PLATFORM = "kraken";
            const string PAIR = "ETHUSD";
            var krakenClient = new KrakenConfig();
            var repo = new ConfigRepository(new List<IExchangeConfig>(){ krakenClient }.ToArray());

            await repo.Deploy(PLATFORM, PAIR);

            var config = await repo.Get(PLATFORM);
            Assert.That(config.Pairs[PAIR].PlatformVariables.ContainsKey("short laverage"), Is.True);
        }


        [Test]
        public async Task  Deploy_With_Client_Variables()
        {
            const string VARIABLE_NAME = "my super variable";
            const string PLATFORM = "test";
            const string PAIR_1 = "ETHUSD";
            const string PAIR_2 = "XBTUSD";
            var client = Substitute.For<IExchangeConfig>();
            client.PairVariables.Returns(new Dictionary<string, object>()
            {
                {VARIABLE_NAME, 156}
            });
            client.Platform.Returns(PLATFORM);
            var repo = new ConfigRepository(new List<IExchangeConfig> {client}.ToArray());

            await repo.Deploy(PLATFORM, PAIR_1, PAIR_2);

            var config = await repo.Get(PLATFORM);
            
            Assert.That(config[PAIR_1].PlatformVariables[VARIABLE_NAME], Is.EqualTo("156"));
            Assert.That(config[PAIR_2].PlatformVariables[VARIABLE_NAME], Is.EqualTo("156"));
        }


        [Test]
        public async Task Get()
        {
            var configRepo = new ConfigRepository(new List<IExchangeConfig>{ new KrakenConfig() }.ToArray());
            var result = await configRepo.Get("kraken");
        }

        [Test]
        public async Task Clean()
        {
           
            
            await _configRepository.CleanUp("kraken");
        }
    }

    
    
}
