﻿using System;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface ILoaderService:IService
    {
        Task<Config> Load();
    }

    public class LoaderService : ILoaderService
    {
        private const string Pair = "ETHUSD";
        private readonly IDatabaseService _databaseService;
        private readonly IExchangeClient[] _clients;
        private readonly IConnectivityService _connectivityService;

        public LoaderService(IDatabaseService databaseService, IExchangeClient[] clients, IConnectivityService connectivityService)
        {
            _databaseService = databaseService;
            _clients = clients;
            _connectivityService = connectivityService;
        }

        public async Task<Config> Load()
        {
            try
            {
                _connectivityService.CheckForInternetConnection();
            }
            catch (Exception)
            {
                var config = await _databaseService.GetConfig();
                config.LoadIntervalMinutes = 10;
                return config;
            }

            var eventName = $"{EventConstant.Load} {Pair}";
            foreach (var client in _clients)
            {
                try
                {            
                    var lastId = await _databaseService.GetLastEventValue(client.Platform, eventName);
                    var getTradesReasult = await client.GetTrades(lastId, Pair);
                    if (!string.IsNullOrEmpty(getTradesReasult.LastId))
                    {
                        await _databaseService.UpdateLastEvent(client.Platform, eventName, getTradesReasult.LastId);
                    }
                }
                catch (Exception e)
                {
                    await _databaseService.Log(client.Platform, $"Error {eventName}", e.Message);
                    await _databaseService.Log(client.Platform, $"Error {eventName}", e.StackTrace);
                }
            }

            return await _databaseService.GetConfig();
        }
    }
}
