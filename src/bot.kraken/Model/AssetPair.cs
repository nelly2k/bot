namespace bot.kraken.Model
{
    /// <summary>
    /// Note: If an asset pair is on a maker/taker fee schedule, the taker side is given 
    /// in "fees" and maker side in "fees_maker". For pairs not on maker/taker, they will only be given in "fees".
    /// 
    /// </summary>
    public class AssetPair
    {
        /// <summary>
        /// alternate pair name
        /// </summary>
        public string Altname { get; set; } 

        public string Aclass_base { get; set; } //asset class of base component
        public string Base { get; set; } //asset id of base component
        public string Aclass_quote { get; set; } //asset class of quote component
        public string Quote { get; set; }
        public string Lot { get; set; }
        
        public int Pair_decimals { get; set; }
        public int Lot_decimals { get; set; }
        public int Lot_multiplier { get; set; }
        public int[] Leverage_buy { get; set; } //amount, fee
        public int[] Leverage_sell { get; set; }//amount, fee
        public decimal[][] Fees { get; set; }//amount, fee
        public decimal[][] Fees_maker { get; set; }//amount, fee
        public string Fee_volume_currency { get; set; }//amount, fee
        public int Margin_call { get; set; }
        public int Margin_stop { get; set; }

    }
}