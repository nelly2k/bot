using System;
using System.Collections.Generic;
using System.Linq;
using bot.model;

namespace bot.core
{
    public class BalanceService
    {
        private List<BalanceItem> balance = new List<BalanceItem>();

        public BalanceService(Config config)
        {
            
        }

        public void Buy(decimal price, decimal volume)
        {
            var existingBalance = balance.FirstOrDefault(x => x.Price == price);
            if (existingBalance != null)
            {
                existingBalance.Volume += volume;
            }
            else
            {
                balance.Add(new BalanceItem(){Volume = volume, Price = price});
            }
        }

        public decimal GetVolumeToSell(decimal? belowPrice=null)
        {
            var itemsBelowPrice = 
            return balance.Where(x => belowPrice== null || x.Price < belowPrice).Sum(x => x.Volume);
        }
        
        public void Sell(decimal volume)
        {
            var sorted = balance.OrderBy(x => x.Price).ToList();
            var leftVol = volume;

            foreach (var item in sorted)
            {
                if (item.Volume > leftVol)
                {
                    item.Volume -= leftVol;
                    return;
                }
                leftVol = leftVol - item.Volume;
                balance.Remove(item);

                if (leftVol == decimal.Zero)
                {
                    return;
                }
            }
        }

        public void MarkNotSold(decimal? priceOver)
        {
            var over = balance.Where(x => x.Price > priceOver);
            foreach (var item in over)
            {
                item.NotSold++;
            }
        }

      
    }
    public class BalanceItem
    {
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public int NotSold { get; set; }

        public DateTime DateTime { get; set; }
        public string Platform { get; set; }
        public string Name { get; set; }
    }

}
