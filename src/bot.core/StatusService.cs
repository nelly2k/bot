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

        public StatusService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<TradeStatus> GetCurrentStatus(string platform, string pair)
        {
            var currentStatusStr = await _eventRepository.GetLastEventValue(platform, $"{EventConstant.StatusUpdate} {pair}");
            var status = (TradeStatus)Enum.Parse(typeof(TradeStatus), currentStatusStr);
            return status;
        }

        public async Task SetCurrentStatus(string platform, TradeStatus tradeStatus, string pair)
        {
            await _eventRepository.UpdateLastEvent(platform, $"{EventConstant.StatusUpdate} {pair}",
                tradeStatus.ToString());
        }

    }
}
