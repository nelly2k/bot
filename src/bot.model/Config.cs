﻿namespace bot.model
{
    public class Config
    {
        public int LoadIntervalMinutes { get; set; } = 3;
        public int AnalyseLoadHours { get; set; } = 12;
        public int AnalyseGroupPeriodMinutes { get; set; } = 4;
        public int AnalyseTresholdMinutes { get; set; } = 30;
        public int AnalyseMacdSlow { get; set; } = 26;
        public int AnalyseMacdFast { get; set; } = 12;
        public int AnalyseMacdSignal { get; set; } = 9;
        public int AnalyseRsiEmaPeriods { get; set; } = 14;
        public int AnalyseRsiLow{ get; set; } = 30;
        public int AnalyseRsiHigh{ get; set; } = 70;

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