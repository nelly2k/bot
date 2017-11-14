using bot.model;
using NUnit.Framework;

namespace bot.core.tests
{
    public class FieldAttributeTests
    {
        [Test]
        public void GetConfigName()
        {
            Assert.That(typeof(Config).Field(x => nameof(x.LoadIntervalMinutes)), Is.EqualTo("load_interval_minutes"));
        }

        [Test]
        public void SetInteger()
        {
            var config = new Config();
            object ob = 154;
            config.Set("load_interval_minutes", ob);

            Assert.That(config.LoadIntervalMinutes, Is.EqualTo(154));
        }

        [Test]
        public void SetBoolean()
        {
            var config = new Config();
            object ob = 1;
            config.Set("is_market", ob);

            Assert.That(config["ETHUSD"].IsMarket, Is.True);
        }
    }
}
