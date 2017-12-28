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

                {FileSessionNames.MACD_Fast_Value,string.Empty },
                {FileSessionNames.MACD_Fast_Signal,string.Empty },
                {FileSessionNames.MACD_Fast_Minutes,string.Empty },
                {FileSessionNames.MACD_Fast_Decision,string.Empty },

                {FileSessionNames.RSI_Peak,string.Empty },
                {FileSessionNames.RSI_Analysis_Minutes,string.Empty },
                {FileSessionNames.RSI_Decision,string.Empty },

                {FileSessionNames.MACD_Slow_Analysis_Value,string.Empty },
                {FileSessionNames.MACD_Slow_Analysis_Signal,string.Empty },
                {FileSessionNames.MACD_Slow_Analysis,string.Empty },

                { FileSessionNames.Analysis,string.Empty },
                { FileSessionNames.Buy_Volume,string.Empty },
                { FileSessionNames.Sell_Volume,string.Empty },
                { FileSessionNames.Borrow_Volume,string.Empty },
                { FileSessionNames.Return_Volume,string.Empty },
                { FileSessionNames.Not_Sold_Volume,string.Empty },
                { FileSessionNames.Profit,string.Empty },
                { FileSessionNames.UsdBalance,string.Empty },
                
            });

            if (!IsStreamFileExists(streamName, "csv", file))
            {
            //    Write(streamName, _config[pair] + Environment.NewLine, false, "csv", file);
                Write(streamName, string.Join(",", sessionDetails[pair].Keys), false, "csv", file);
            }

        }

        public void GatherDetails(string pair, string name, double value)
        {
            double.TryParse(sessionDetails[pair][name], out var newdata);
            GatherDetails(pair, name, Math.Round(newdata + value, 2).ToString());
        }

        public void GatherDetails(string pair, string name, decimal value)
        {
            decimal.TryParse(sessionDetails[pair][name], out var newdata);
            GatherDetails(pair, name, Math.Round(newdata + value, 2).ToString());
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
        public static string Status => "Status";

        public static string PriceSell => "Price Sell";
        public static string PriceBuy => "Price Buy";
        public static string Volume => "Volume";

        public static string MACD_Fast_Value => "MACDF Value";
        public static string MACD_Fast_Signal => "MACDF Signal";
        public static string MACD_Fast_Minutes => "MACDF Since";
        public static string MACD_Fast_Decision => "MACDF";

        public static string RSI_Peak => "RSI Peak";
        public static string RSI_Analysis_Minutes => "RSI Time";
        public static string RSI_Decision => "RSI";

        public static string MACD_Slow_Analysis_Value => "MACDS Value";
        public static string MACD_Slow_Analysis_Signal => "MACDS Signal";
        public static string MACD_Slow_Analysis => "MACDS";

        public static string Analysis => "Analysis";
        public static string Buy_Volume => "Buy";
        public static string Sell_Volume => "Sell";
        public static string Borrow_Volume => "Borrow";
        public static string Return_Volume =>"Return";
        public static string Not_Sold_Volume => "Not Sold";
        public static string Profit => "Profit";
        public static string UsdBalance => "Usd Balance";
        
        
    }
}
