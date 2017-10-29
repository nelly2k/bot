using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace bot.core.tests
{
    public class WheenStatus
    {
        [Test]
        public void ParsingStatus()
        {
            Assert.That((TradeStatus)Enum.Parse(typeof(TradeStatus), "Unknown"),Is.EqualTo(TradeStatus.Unknown));
        }
    }
}
