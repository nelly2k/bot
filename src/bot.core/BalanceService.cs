using System;
using System.Collections.Generic;
using System.Linq;
using bot.model;

namespace bot.core
{
  
    public class BalanceItem
    {
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public int NotSold { get; set; }

        public DateTime DateTime { get; set; }
        public DateTime BoughtDate { get; set; }
        public DateTime? NotSoldtDate { get; set; }
        public string Platform { get; set; }
        public string Name { get; set; }
        public bool IsBorrowed { get; set; }
        
    }

}
