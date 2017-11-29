using bot.model;
using NUnit.Framework;

namespace bot.core.tests
{
    public class FieldAttributeTests
    {
        [Test]
        public void GetConfigName()
        {
            Assert.That(typeof(Config).GetField(x => nameof(x.LoadIntervalMinutes)), Is.EqualTo("load interval minutes"));
        }

        [Test]
        public void SetInteger()
        {
            var config = new Config();
            object ob = 154;
            config.SetField("load interval minutes", ob);

            Assert.That(config.LoadIntervalMinutes, Is.EqualTo(154));
        }

        [Test]
        public void SetString()
        {
            var config = new Config();
            object ob = "ZUSD";
            config.SetField("base currency", ob);

            Assert.That(config["ETHUSD"].IsMarket, Is.True);
        }

        [Test]
        public void SetBoolean()
        {
            var config = new PairConfig();
            object ob = true;
            config.SetField("is load", ob);

            Assert.That(config.ShouldLoad, Is.True);
        }
    }
}
