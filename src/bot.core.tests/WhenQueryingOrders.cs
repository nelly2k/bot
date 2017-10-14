using System.Threading.Tasks;
using NUnit.Framework;

namespace bot.core.tests
{
    public class WhenQueryingOrders
    {
        [Test]
        public async Task AddGetRemove()
        {
            var repo = new OrderRepository();
            await repo.Add("testPlatform", "testPair", "testOrderId");

            var getResult = await repo.Get("testPlatform");
            Assert.That(getResult.Count, Is.EqualTo(1));

            await repo.Remove("testPlatform", "testOrderId");

            var getResult2 = await repo.Get("testPlatform");
            
            Assert.That(getResult2.Count, Is.EqualTo(0));
        }
    }
}
