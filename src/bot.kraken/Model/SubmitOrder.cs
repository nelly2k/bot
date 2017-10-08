using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bot.kraken.Model
{
    public class SubmitOrder
    {
        //Asset pair
        public string Pair { get; set; }
        //Type of order (buy or sell)
        public string Type { get; set; }
        //Execution type
        public string OrderType { get; set; }
        //Price. Optional. Dependent upon order type
        public decimal? Price { get; set; }
        //Secondary price. Optional. Dependent upon order type
        public decimal? Price2 { get; set; }
        //Order volume in lots
        public decimal Volume { get; set; }
        //Amount of leverage required. Optional. default none
        public string Leverage { get; set; }
        //Position tx id to close (optional.  used to close positions)
        public string Position { get; set; }
        //list of order flags (optional):
        public string OFlags { get; set; }
        //Scheduled start time. Optional
        public string Starttm { get; set; }
        //Expiration time. Optional
        public string Expiretm { get; set; }
        //User ref id. Optional
        public string Userref { get; set; }
        //Validate inputs only. do not submit order. Optional
        public bool Validate { get; set; }
        //Closing order details
        public Dictionary<string, string> Close { get; set; }
    }


    public enum OFlag
    {
        viqc, //volume in quote currency
        nompp = 3, //no market price protection,
        fciq, //prefer fee in quote currency (default if buying)
        fcib // prefer fee in quote currency (default if buying)
    }
}
