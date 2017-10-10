using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public class LoaderService
    {
        const string pair = "ETHUSD";
        private readonly IDatabaseService _databaseService;
        private readonly IExchangeClient[] _clients;
        private readonly IConnectivityService _connectivityService;

        public LoaderService(IDatabaseService databaseService, IExchangeClient[] clients, IConnectivityService connectivityService)
        {
            _databaseService = databaseService;
            _clients = clients;
            _connectivityService = connectivityService;
        }

        public async Task Load()
        {
            foreach (var client in _clients)
            {
                _connectivityService.CheckForInternetConnection();
                var lastId = await _databaseService.GetLastEventValue("kraken load "+ pair);
                var getTradesReasult = await client.GetTrades(lastId, pair);
                if (!string.IsNullOrEmpty(getTradesReasult.LastId))
                {
                    await _databaseService.UpdateLastEvent("kraken load " + pair, getTradesReasult.LastId);
                }

            }
        }
    }
}
