using System.Linq;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface IOrderService
    {
        Task CheckOpenOrders();
        Task CheckOpenOrders(IExchangeClient client);
        Task Buy(IExchangeClient client, string pair, decimal price);
        Task Sell(IExchangeClient client, string pair, decimal price);
    }

    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IExchangeClient[] _clients;
        private readonly IBalanceRepository _balanceRepository;
        private readonly Config _config;
        private readonly ILogRepository _logRepository;
        private readonly IMoneyService _moneyService;
        private readonly INotSoldRepository _notSoldRepository;

        public OrderService(IOrderRepository orderRepository, IExchangeClient[] clients, IBalanceRepository balanceRepository, Config config,
            ILogRepository logRepository, IMoneyService moneyService, INotSoldRepository notSoldRepository)
        {
            _orderRepository = orderRepository;
            _clients = clients;
            _balanceRepository = balanceRepository;
            _config = config;
            _logRepository = logRepository;
            _moneyService = moneyService;
            _notSoldRepository = notSoldRepository;
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

        public async Task Buy(IExchangeClient client, string pair, decimal price)
        {
            var currentBalance = await client.GetBaseCurrencyBalance();
            var moneyToSpend = currentBalance / 100m * (decimal) _config.PairPercent[pair];
            if (moneyToSpend < _config.MinBuyBaseCurrency)
            {
                await _logRepository.Log(client.Platform, "trade error",
                    $"[Insufficient funds. Base currency balance:{currentBalance}]");
                return;
            }

            var transformResult = _moneyService.Transform(moneyToSpend, price, 0.26m);

            var orderIds = await client.AddOrder(OrderType.buy, transformResult.TargetCurrencyAmount, price);
            await _logRepository.Log(client.Platform, "Trade",
                $"Added buy order for [pair:{pair}] [volume:{transformResult.TargetCurrencyAmount}]");

            foreach (var orderId in orderIds)
            {
                await _orderRepository.Add(client.Platform, pair, orderId);
            }
        }

        public async Task Sell(IExchangeClient client, string pair, decimal price)
        {
            var balanceItems = await _balanceRepository.Get(client.Platform, pair);

            decimal volume = decimal.Zero;

            foreach (var balanceItem in balanceItems)
            {
                var boughtPrice = balanceItem.Volume * balanceItem.Price + _moneyService.FeeToPay(balanceItem.Volume, balanceItem.Price, 0.16m);
                var sellPrice = balanceItem.Volume * price +
                                _moneyService.FeeToPay(balanceItem.Volume, balanceItem.Price, 0.26m);
                
                if (boughtPrice < sellPrice || balanceItem.NotSold >= _config.MaxMissedSells)
                {
                    volume += balanceItem.Volume;
                }
                else
                {
                    await _notSoldRepository.SetNotSold(client.Platform, pair);
                }
            }

            if (volume == decimal.Zero)
            {
                return;
            }

            var orderIds = await client.AddOrder(OrderType.sell, volume, price);
            await _logRepository.Log(client.Platform, "Trade",
                $"Added sell order for [pair:{pair}] [volume:{volume}]");

            foreach (var orderId in orderIds)
            {
                await _orderRepository.Add(client.Platform, pair, orderId);
            }

        }
    }
}
