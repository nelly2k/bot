using System.Linq;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public class OrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IExchangeClient[] _clients;
        private readonly IBalanceRepository _balanceRepository;
        private readonly Config _config;
        private readonly ILogRepository _logRepository;

        public OrderService(IOrderRepository orderRepository, IExchangeClient[] clients, IBalanceRepository balanceRepository, Config config,
            ILogRepository logRepository)
        {
            _orderRepository = orderRepository;
            _clients = clients;
            _balanceRepository = balanceRepository;
            _config = config;
            _logRepository = logRepository;
        }

        public async Task CheckOpenOrders()
        {
            foreach (var client in _clients)
            {
                await CheckOpenOrders(client);
            }
        }

        public async Task CheckOpenOrders(IExchangeClient client)
        {
            var openOrders = await _orderRepository.Get(client.Platform);
            var orders = await client.GetOrders(openOrders.Keys.ToArray());

            foreach (var order in orders.Where(x=>x.OrderStatus == OrderStatus.Closed))
            {
                await _balanceRepository.Add(client.Platform, order.Pair, order.Volume, order.Price);
                await _orderRepository.Remove(client.Platform, order.Id);
            }
        }

        public async Task Buy(IExchangeClient client, string pair)
        {
            var currentBalance = await client.GetBaseCurrencyBalance();
            var moneyToSpend = currentBalance / 100m * (decimal) _config.PairPercent[pair];
            if (moneyToSpend < _config.MinBuyBaseCurrency)
            {
                await _logRepository.Log(client.Platform, "insufficient fund",
                    $"[Base currency balance:{currentBalance}]");
                return;
            }

            client

        }
    }
}
