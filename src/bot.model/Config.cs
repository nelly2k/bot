﻿using System.Collections.Generic;

namespace bot.model
{
    public class Config:IApiCredentials
    {

        public int LoadIntervalMinutes { get; set; } = 3;
        public int AnalyseLoadHours { get; set; } = 12;
        public int AnalyseGroupPeriodMinutes { get; set; } = 4;
        public int AnalyseMacdGroupPeriodMinutesSlow { get; set; } = 20;
        public int AnalyseTresholdMinutes { get; set; } = 30;
        public int AnalyseMacdSlow { get; set; } = 26;
        public int AnalyseMacdFast { get; set; } = 12;
        public int AnalyseMacdSignal { get; set; } = 9;
        public int AnalyseRsiEmaPeriods { get; set; } = 14;
        public int AnalyseRsiLow{ get; set; } = 30;
        public int AnalyseRsiHigh{ get; set; } = 70;
        public int MinBuyBaseCurrency{ get; set; } = 2;
        public int MaxMissedSells { get; set; } = 3;
        public string BaseCurrency { get; set; } = "ZUSD";
        public decimal AnalyseMacdSlowThreshold { get; set; } = 0m;

        public List<string> PairToLoad { get; set; } = new List<string>();

        public Dictionary<string, double> PairPercent { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, decimal> MinVolume { get; set; } = new Dictionary<string, decimal>();

        public string Key{ get; set; } 
        public string Secret{ get; set; }

        public bool IsMarket { get; set; } = false;

        public override string ToString()
        {
            return $"Config [LoadIntervalMinutes:{LoadIntervalMinutes}] " +
                   $"[AnalyseLoadHours:{AnalyseLoadHours}] " +
                   $"[AnalyseGroupPeriodMinutes:{AnalyseGroupPeriodMinutes}] " +
                   $"[AnalyseTresholdMinutes:{AnalyseTresholdMinutes}] " +
                   $"[AnalyseMacdSlow:{AnalyseMacdSlow}] " +
                   $"[AnalyseMacdFast:{AnalyseMacdFast}] " +
                   $"[AnalyseMacdSignal:{AnalyseMacdSignal}] " +
                   $"[AnalyseRsiEmaPeriods:{AnalyseRsiEmaPeriods}] " +
                   $"[AnalyseRsiLow:{AnalyseRsiLow}] " +
                   $"[AnalyseRsiHigh:{AnalyseRsiHigh}] ";
        }
    }
}
