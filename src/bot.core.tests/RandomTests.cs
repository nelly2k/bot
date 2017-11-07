using System;
using bot.model;
using Microsoft.Practices.Unity;
using NUnit.Framework;

namespace bot.core.tests
{
    public class RandomTests
    {
        [Test]
        public void Rand()
        {

            var rnd = new Random();
            var a = rnd.Next();

            var rnd1= new Random();
            var a1 = rnd1.Next();

            Assert.That(a, Is.Not.EqualTo(a1));

        }

        [Test]
        public void UnityRandom()
        {
            var container = new UnityContainer();
            container.RegisterInstance<IRandom>(new MyRandom());

            var rnd = container.Resolve<IRandom>();
            var a = rnd.Get();

            var rnd2 = container.Resolve<IRandom>();
            var a1 = rnd2.Get();

            Assert.That(a, Is.Not.EqualTo(a1));

        }
    }
}
