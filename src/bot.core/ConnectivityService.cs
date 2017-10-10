using System;
using System.Net;
using bot.model;

namespace bot.core
{
    public interface IConnectivityService:IService
    {
        void CheckForInternetConnection();
    }

    public class ConnectivityService : IConnectivityService
    {
        public void CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (client.OpenRead("http://clients3.google.com/generate_204"))
                    {
                        
                    }
                }
            }
            catch
            {
                throw new ConnectionNoAvailableException();
            }
        }
    }

    public class ConnectionNoAvailableException : Exception
    {
        
    }
}
