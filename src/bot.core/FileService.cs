using System;
using System.Collections.Generic;
using System.IO;
using bot.model;

namespace bot.core
{
    public interface IFileService : IService
    {

        void Write(string strem, Exception ex);

        void StartSession(string pair);
        void GatherDetails(string pair, string name, string value);
        void CloseSession(string pair);
        void Write(string stream, string message, bool isDate = true, string extention = "txt", string uinifiedFile = "");
        void GatherDetails(string pair, string name, decimal value);
        void GatherDetails(string pair, string name, double value);
    }

    public class FileService : IFileService
    {
        private readonly IDateTime _dateTime;
        private readonly Config _config;

        public FileService(IDateTime dateTime, Config config)
        {
            _dateTime = dateTime;
            _config = config;
        }

        public void Write(string message)
        {
            using (var file = new StreamWriter($"h:\\{_config.LogPrefix}_{_dateTime.Now:yyMMdd}.txt", true))
            {
                file.WriteLine($"{_dateTime.Now:t}| {message}");
                file.Close();
            }
        }

        private string GetStreamFilename(string stream, string extention = "txt", string uinifiedFile = "")
        {
            var date = string.IsNullOrEmpty(uinifiedFile) ? _dateTime.Now.ToString("yyMMdd") : uinifiedFile;

            return $"h:\\{_config.LogPrefix}_{stream}_{date}.{extention}";
        }

        private bool IsStreamFileExists(string stream, string extention, string uinifiedFile)
        {
            return File.Exists(GetStreamFilename(stream, extention, uinifiedFile));
        }

        public void Write(string stream, string message, bool isDate = true, string extention = "txt", string uinifiedFile = "")
        {
            var date = isDate ? $"{_dateTime.Now:t}| " : string.Empty;
            using (var file = new StreamWriter(GetStreamFilename(stream, extention, uinifiedFile), true))
            {
                file.WriteLine($"{date}{message}");
                file.Close();
            }
        }

        public void Write(string strem, Exception ex)
        {
            Write(strem, ex.Message);
            Write(strem, ex.StackTrace);

            if (ex.InnerException != null)
            {
                Write(strem, ex.InnerException);
            }
        }

        private Dictionary<string, Dictionary<string, string>> sessionDetails = new Dictionary<string, Dictionary<string, string>>();

        public void StartSession(string pair)
        {
            var file = DateTime.Now.ToString("yyMMdd");
            var streamName = $"session_{pair}";
            
           
            sessionDetails.Add(pair, new Dictionary<string, string>
            {
                {FileSessionNames.Date,string.Empty },
                {FileSessionNames.Status,string.Empty },
                {FileSessionNames.PriceSell,string.Empty },
                {FileSessionNames.PriceBuy,string.Empty },
                {FileSessionNames.Volume,string.Empty },

                {FileSessionNames.MACD_Fast_Analysis,string.Empty },
                {FileSessionNames.MACD_Fast_Value,string.Empty },
                {FileSessionNames.MACD_Fast_Signal,string.Empty },
                {FileSessionNames.MACD_Fast_Minutes,string.Empty },
                {FileSessionNames.MACD_Fast_Decision,string.Empty },

                {FileSessionNames.RSI_Analysis,string.Empty },
                {FileSessionNames.RSI_Peak,string.Empty },
                {FileSessionNames.RSI_Analysis_Minutes,string.Empty },
                {FileSessionNames.RSI_Decision,string.Empty },

                {FileSessionNames.MACD_Slow_Analysis,string.Empty },
                {FileSessionNames.MACD_Slow_Analysis_Value,string.Empty },
                {FileSessionNames.MACD_Slow_Analysis_Signal,string.Empty },

                { FileSessionNames.Analysis,string.Empty },
                { FileSessionNames.Buy_Volume,string.Empty },
                { FileSessionNames.Sell_Volume,string.Empty },
                { FileSessionNames.Borrow_Volume,string.Empty },
                { FileSessionNames.Return_Volume,string.Empty },
                { FileSessionNames.Not_Sold_Volume,string.Empty },
                { FileSessionNames.CoinBalance,string.Empty },
                { FileSessionNames.UsdBalance,string.Empty }
            });

            if (!IsStreamFileExists(streamName, "csv", file))
            {
                Write(streamName, _config[pair] + Environment.NewLine, false, "csv", file);
                Write(streamName, string.Join(",", sessionDetails[pair].Keys), false, "csv", file);
            }

        }

        public void GatherDetails(string pair, string name, double value)
        {
            GatherDetails(pair, name, Math.Round(value, 3).ToString());
        }

        public void GatherDetails(string pair, string name, decimal value)
        {
            GatherDetails(pair, name, Math.Round(value, 3).ToString());
        }

        public void GatherDetails(string pair, string name, string value)
        {
            sessionDetails[pair][name] = value;
        }

        public void CloseSession(string pair)
        {
            var file = DateTime.Now.ToString("yyMMdd");
            var streamName = $"session_{pair}";
            Write(streamName, string.Join(",", sessionDetails[pair].Values), false, "csv", file);

            sessionDetails.Remove(pair);
        }
    }

    public class FileSessionNames
    {
        public static string Date => "Date";
        public static string PriceSell => "PriceSell";
        public static string PriceBuy => "PriceBuy";
        public static string Volume => "Volume";
        public static string MACD_Fast_Analysis => "MACD F Analysis";
        public static string MACD_Fast_Minutes => "MACD F Minutes";
        public static string MACD_Fast_Value => "MACD F Value";
        public static string MACD_Fast_Signal => "MACD F Signal";
        public static string MACD_Fast_Decision => "MACD F Decision";

        public static string RSI_Analysis_Minutes => "RSI Minutes";
        public static string RSI_Peak => "RSI Peak";
        public static string RSI_Analysis => "RSI";
        public static string RSI_Decision => "RSI Decision";

        public static string MACD_Slow_Analysis => "MACD S Analysis";
        public static string MACD_Slow_Analysis_Value => "MACD S Value";
        public static string MACD_Slow_Analysis_Signal => "MACD S Signal";
        public static string Analysis => "Analysis";
        public static string Buy_Volume => "Buy Volume";
        public static string Sell_Volume => "Sell Volume";
        public static string Borrow_Volume => "Borrow Volume";
        public static string Return_Volume =>"Return Volume";
        public static string Not_Sold_Volume => "Not Sold Volume";
        public static string CoinBalance => "Coin Balance";
        public static string UsdBalance => "Usd Balance";
        public static string Status => "Status";
    }
}
