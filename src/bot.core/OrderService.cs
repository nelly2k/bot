using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface IOrderService:IService
    {
        Task CheckOpenOrders();
        Task CheckOpenOrders(IExchangeClient client);
        Task Buy(IExchangeClient client, string pair, decimal price, bool isMarket = false);
        Task Sell(IExchangeClient client, string pair, decimal price, bool isMarket = false);
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

        public OrderService(IOrderRepository orderRepository, IExchangeClient[] clients, IBalanceRepository balanceRepository, Config config,
            ILogRepository logRepository, IMoneyService moneyService, INotSoldRepository notSoldRepository, IFileService fileService)
        {
            _orderRepository = orderRepository;
            _clients = clients;
            _balanceRepository = balanceRepository;
            _config = config;
            _logRepository = logRepository;
            _moneyService = moneyService;
            _notSoldRepository = notSoldRepository;
            _fileService = fileService;
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
            if (openOrders==null || !openOrders.Any())
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

        public async Task Buy(IExchangeClient client, string pair, decimal price, bool isMarket = false)
        {
            var currentBalance = await client.GetBaseCurrencyBalance();
            var moneyToSpend = currentBalance / 100m * (decimal)_config.PairPercent[pair];
            if (moneyToSpend < _config.MinBuyBaseCurrency)
            {
                await _logRepository.Log(client.Platform, "trade error",
                    $"[Insufficient funds. Base currency balance:{currentBalance}]");
                return;
            }

            var transformResult = _moneyService.Transform(moneyToSpend, price, 0.26m);
            List<string> orderIds;
            if (isMarket)
            {
                _fileService.Write(pair, $"Buy [volume:{transformResult.TargetCurrencyAmount}]");
                orderIds = await client.AddOrder(OrderType.buy, transformResult.TargetCurrencyAmount, pair:pair);
            }
            else
            {
                _fileService.Write(pair, $"Buy [volume:{transformResult.TargetCurrencyAmount}] [price:{price}]");
                orderIds = await client.AddOrder(OrderType.buy, transformResult.TargetCurrencyAmount, price, pair: pair);
            }

            await _logRepository.Log(client.Platform, "Trade",
                $"Added buy order for [pair:{pair}] [volume:{transformResult.TargetCurrencyAmount}]");

            foreach (var orderId in orderIds)
            {
                await _orderRepository.Add(client.Platform, pair, orderId);
            }
        }

        public async Task Sell(IExchangeClient client, string pair, decimal price, bool isMarket = false)
        {
            var balanceItems = await _balanceRepository.Get(client.Platform, pair);

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
                    _fileService.Write(pair, $"Not worths to sell [not sold: {balanceItem.NotSold}");
                    await _notSoldRepository.SetNotSold(client.Platform, pair);
                }
            }

            if (volume == decimal.Zero)
            {
                return;
            }
            List<string> orderIds;
            if (isMarket)
            {

                _fileService.Write(pair, $"Sell [volume:{volume}] [price:{price}]");
                orderIds = await client.AddOrder(OrderType.sell, volume, pair: pair);
            }
            else
            {
                _fileService.Write(pair, $"Sell [volume:{volume}]");
                orderIds = await client.AddOrder(OrderType.sell, volume, price, pair: pair);
            }
            await _logRepository.Log(client.Platform, "Trade",
                $"Added sell order for [pair:{pair}] [volume:{volume}]");

            foreach (var orderId in orderIds)
            {
                await _orderRepository.Add(client.Platform, pair, orderId);
            }

        }
    }
}
