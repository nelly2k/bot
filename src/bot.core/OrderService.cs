using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface IOrderService : IService
    {
        Task CheckOpenOrders();
        Task CheckOpenOrders(IExchangeClient client);
        Task Buy(IExchangeClient client, string pair, decimal price);
        Task<bool> Sell(IExchangeClient client, string pair, decimal price);
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
        private readonly IFileService _fileService;
        private readonly IDateTime _dateTime;

        public OrderService(IOrderRepository orderRepository, IExchangeClient[] clients, IBalanceRepository balanceRepository, Config config,
            ILogRepository logRepository, IMoneyService moneyService, INotSoldRepository notSoldRepository, IFileService fileService, IDateTime dateTime)
        {
            _orderRepository = orderRepository;
            _clients = clients;
            _balanceRepository = balanceRepository;
            _config = config;
            _logRepository = logRepository;
            _moneyService = moneyService;
            _notSoldRepository = notSoldRepository;
            _fileService = fileService;
            _dateTime = dateTime;
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
            if (openOrders == null || !openOrders.Any())
            {
                return;
            }
            var orders = await client.GetOrders(openOrders.Keys.ToArray());

            foreach (var order in orders.Where(x => x.OrderStatus == OrderStatus.Closed))
            {
                if (order.OrderType == OrderType.buy)
                {
                    await _balanceRepository.Add(client.Platform, order.Pair, order.Volume, order.Price);
                }
                else
                {
                    await _balanceRepository.Remove(client.Platform, order.Pair);
                }

                await _orderRepository.Remove(client.Platform, order.Id);
            }
        }

        public async Task Buy(IExchangeClient client, string pair, decimal price)
        {
            var currentBalance = await client.GetBaseCurrencyBalance();
            var moneyToSpend = currentBalance / 100m * (decimal)_config.PairPercent[pair];

            var transformResult = _moneyService.Transform(moneyToSpend, price, 0.26m);
            if (transformResult.TargetCurrencyAmount < _config.MinVolume[pair])
            {
                _fileService.Write(pair, $"Insufficient funds [volume:{transformResult.TargetCurrencyAmount}]");
                return;
            }
            _fileService.Write(pair, $"Buy [volume:{transformResult.TargetCurrencyAmount}] [price:{price}]");
            var orderIds = await client.AddOrder(OrderType.buy, transformResult.TargetCurrencyAmount, pair, _config.IsMarket ? (decimal?)null : price);

            foreach (var orderId in orderIds)
            {
                await _orderRepository.Add(client.Platform, pair, orderId);
            }
        }

        public async Task<bool> Sell(IExchangeClient client, string pair, decimal price)
        {
            var balanceItems = await _balanceRepository.Get(client.Platform, pair);
            var isNotSold = false;
            decimal volume = decimal.Zero;
            foreach (var balanceItem in balanceItems)
            {
                var boughtPrice = balanceItem.Volume * balanceItem.Price + _moneyService.FeeToPay(balanceItem.Volume, balanceItem.Price, 0.26m);
                var sellPrice = balanceItem.Volume * price +
                                _moneyService.FeeToPay(balanceItem.Volume, balanceItem.Price, 0.26m);

                if (boughtPrice < sellPrice || balanceItem.NotSold >= _config.MaxMissedSells)
                {
                    volume += balanceItem.Volume;
                }
                else
                {
                    if (balanceItem.NotSoldtDate < _dateTime.Now.AddMinutes(_config.AnalyseTresholdMinutes))
                    {
                        _fileService.Write(pair, $"Not worths to sell, and too short.");
                    }
                    else
                    {
                        _fileService.Write(pair, $"Not worths to sell [not sold:{balanceItem.NotSold}]");
                        await _notSoldRepository.SetNotSold(client.Platform, pair);
                    }

                    isNotSold = true;
                }
            }

            if (volume == decimal.Zero)
            {
                return !isNotSold;
            }
            _fileService.Write(pair, $"Sell [volume:{volume}]");
            var orderIds = await client.AddOrder(OrderType.sell, volume, pair, _config.IsMarket ? (decimal?)null : price);
            if (orderIds == null || !orderIds.Any())
            {
                return false;
            }
            foreach (var orderId in orderIds)
            {
                _fileService.Write(pair, $"Order submitted [id:{orderId}]");
                await _orderRepository.Add(client.Platform, pair, orderId);
            }
            return true;
        }
    }
}
