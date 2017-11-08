using System;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface IStatusService:IService
    {
        Task<TradeStatus> GetCurrentStatus(string platform, string pair);
        Task SetCurrentStatus(string platform, TradeStatus tradeStatus, string pair);
    }

    public class StatusService : IStatusService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IFileService _fileService;

        public StatusService(IEventRepository eventRepository, IFileService fileService)
        {
            _eventRepository = eventRepository;
            _fileService = fileService;
        }

        public async Task<TradeStatus> GetCurrentStatus(string platform, string pair)
        {
            var currentStatusStr = await _eventRepository.GetLastEventValue(platform, $"{EventConstant.StatusUpdate} {pair}");
            var status = (TradeStatus)Enum.Parse(typeof(TradeStatus), currentStatusStr);
            _fileService.Write(pair, $"Get status for [platform:{platform}] [status:{status}]");
            return status;
        }

        public async Task SetCurrentStatus(string platform, TradeStatus tradeStatus, string pair)
        {
            _fileService.Write(pair, $"Set status for [platform:{platform}] [new status:{tradeStatus}]");
            await _eventRepository.UpdateLastEvent(platform, $"{EventConstant.StatusUpdate} {pair}",
                tradeStatus.ToString());
        }

    }
}
