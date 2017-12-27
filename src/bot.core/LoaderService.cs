using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface ILoaderService:IService
    {
        Task Load();
    }

    public class LoaderService : ILoaderService
    {
        private readonly ITradeRepository _tradeRepository;
        private readonly IExchangeClient[] _clients;
        private readonly IEventRepository _eventRepository;
        private readonly ILogRepository _logRepository;
        private readonly Config _config;
        private readonly IFileService _fileService;

        public LoaderService(ITradeRepository tradeRepository, IExchangeClient[] clients,
            IEventRepository eventRepository,ILogRepository logRepository, Config config, IFileService fileService)
        {
            _tradeRepository = tradeRepository;
            _clients = clients;
            _eventRepository = eventRepository;
            _logRepository = logRepository;
            _config = config;
            _fileService = fileService;
        }

        public async Task Load()
        {
            var pairs = _config.Pairs.Where(x=>x.Value.ShouldLoad).Select(x=>x.Key);

            foreach (var pair in pairs)
            {
                var eventName = $"{EventConstant.Load} {pair}";
                foreach (var client in _clients)
                {
                    try
                    {
                        var lastId = await _eventRepository.GetLastEventValue(client.Platform, eventName);
                        var getTradesReasult = await client.GetTrades(lastId, pair);
                        await _tradeRepository.SaveTrades(getTradesReasult.Results, pair);
                        await _eventRepository.UpdateLastEvent(client.Platform, eventName, getTradesReasult.LastId);
                    }
                    catch (Exception e)
                    {
                        _fileService.Write(pair, e);
                    }
                }
            }
           
        }
    }
}
