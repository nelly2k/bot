using System;
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
        Task CheckOperations(IExchangeClient client);
        Task Borrow(IExchangeClient client, string pair, decimal price);
        Task<bool> Return(IExchangeClient client, string pair, decimal price);
    }

    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IExchangeClient[] _clients;
        private readonly IBalanceRepository _balanceRepository;
        private readonly Config _config;
        private readonly IMoneyService _moneyService;
        private readonly INotSoldRepository _notSoldRepository;
        private readonly IFileService _fileService;
        private readonly IDateTime _dateTime;
        private readonly IOperationRepository _operationRepository;
        private readonly IStatusService _statusService;

        public OrderService(IOrderRepository orderRepository, IExchangeClient[] clients, IBalanceRepository balanceRepository, Config config,
            IMoneyService moneyService, INotSoldRepository notSoldRepository, IFileService fileService, IDateTime dateTime, IOperationRepository operationRepository,
            IStatusService statusService)

        {
            _orderRepository = orderRepository;
            _clients = clients;
            _balanceRepository = balanceRepository;
            _config = config;
            _moneyService = moneyService;
            _notSoldRepository = notSoldRepository;
            _fileService = fileService;
            _dateTime = dateTime;
            _operationRepository = operationRepository;
            _statusService = statusService;
        }

        public async Task CheckOpenOrders()
        {
            foreach (var client in _clients)
            {
                await CheckOpenOrders(client);
            }
        }

        public async Task CheckOperations(IExchangeClient client)
        {
            var incompleteOperations = (await _operationRepository.GetIncomplete(client.Platform, "add order")).OrderBy(x=>x.Id);
           
            foreach (var operation in incompleteOperations)
            {
                _fileService.Write(operation.Pair, $"Incomplete operation [id:{operation.Id}] [operation:{operation.Misc}]");
                var orderIds = await client.GetOrdersIds(operation.Id);
                if (!orderIds.Any())
                {
                    if ((_dateTime.Now - operation.OperationDate).TotalMinutes > 20)
                    {
                        await _operationRepository.Complete(operation.Id);
                    }
                    continue;
                }

                foreach (var orderId in orderIds)
                {
                    await _orderRepository.Add(client.Platform, operation.Pair, orderId);
                }

                await _operationRepository.Complete(operation.Id);
                if (operation.Misc == "sell" || operation.Misc == "borrow")
                {
                    await _statusService.SetCurrentStatus(client.Platform, TradeStatus.Sell, operation.Pair);
                }else if (operation.Misc == "buy" || operation.Misc == "return")
                {
                    await _statusService.SetCurrentStatus(client.Platform, TradeStatus.Buy, operation.Pair);
                }
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

            foreach (var order in orders.Where(x => x.OrderStatus == OrderStatus.Closed || x.OrderStatus == OrderStatus.Cancelled))
            {
                if (order.OrderStatus != OrderStatus.Cancelled)
                {
                    if (order.OrderType == OrderType.buy)
                    {
                        if (order.IsBorrowed)
                        {
                            await _balanceRepository.Remove(client.Platform, order.Pair, true);
                        }
                        else
                        {
                            await _balanceRepository.Add(client.Platform, order.Pair, order.Volume, order.Price, false);
                        }

                    }
                    else
                    {

                        if (order.IsBorrowed)
                        {
                            await _balanceRepository.Add(client.Platform, order.Pair, order.Volume, order.Price, true);
                        }
                        else
                        {
                            await _balanceRepository.Remove(client.Platform, order.Pair, false);
                        }

                    }
                }
              
                    await _orderRepository.Remove(client.Platform, order.Id);
              
            }
        }

        public async Task Borrow(IExchangeClient client, string pair, decimal price)
        {
            var balanceItems = (await _balanceRepository.Get(client.Platform, pair)).Where(x => x.IsBorrowed);
            if (balanceItems.Any())
            {
                return;
            }
            var availableVolume = Math.Round(await client.GetAvailableMargin(pair.Substring(0, 3)),_config[pair].VolumeFormat);
            //availableVolume = availableVolume / 100m * (decimal)_config[pair].Share;
            if (availableVolume < _config[pair].MinVolume)
            {
                //Insufficient funds
                return;
            }

            _fileService.GatherDetails(pair, FileSessionNames.Borrow_Volume, availableVolume);
            var operationId = await _operationRepository.Add(client.Platform, "add order", pair, "borrow");
            var orderIds = await client.Sell(availableVolume, pair, _config[pair].IsMarket ? (decimal?)null : price, operationId, true);

            if (orderIds == null || !orderIds.Any())
            {
                return;
            }
            await _operationRepository.Complete(operationId);
            foreach (var orderId in orderIds)
            {
                await _orderRepository.Add(client.Platform, pair, orderId);
            }
        }
        
        public async Task<bool> Return(IExchangeClient client, string pair, decimal price)
        {
            var balanceItems = (await _balanceRepository.Get(client.Platform, pair)).Where(x => x.IsBorrowed);
            var isNotReturned = false;
            var volume = decimal.Zero;
            var profit = decimal.Zero;
            foreach (var balanceItem in balanceItems)
            {
                var borrowedPrice = balanceItem.Volume * balanceItem.Price
                                  - _moneyService.FeeToPay(pair, balanceItem.Volume, balanceItem.Price, 0.26m)
                                  - _moneyService.FeeToPay(pair, balanceItem.Volume, price, 0.26m);
                var returnPrice = balanceItem.Volume * price;
                if (borrowedPrice > returnPrice || balanceItem.NotSold >= _config[pair].MaxMissedSells)
                {
                    volume += balanceItem.Volume;
                    profit += borrowedPrice - returnPrice;
                }
                else
                {
                    if (balanceItem.NotSoldtDate > _dateTime.Now.AddMinutes(-_config[pair].ThresholdMinutes * balanceItem.NotSold + 1))
                    {
                        //_fileService.Write(pair, $"Not worths to sell, and too short.");
                    }
                    else
                    {
                        _fileService.GatherDetails(pair, FileSessionNames.Not_Sold_Volume, balanceItem.Volume);
                        await _notSoldRepository.SetNotSold(client.Platform, pair, true);
                    }

                    isNotReturned = true;
                }
            }

            if (volume == decimal.Zero)
            {
                return !isNotReturned;
            }
            _fileService.GatherDetails(pair, FileSessionNames.Return_Volume, volume);
            _fileService.GatherDetails(pair, FileSessionNames.Profit, profit);

            var operationId = await _operationRepository.Add(client.Platform, "add order", pair, "return");
            
            var orderIds = await client.Buy(volume, pair, _config[pair].IsMarket ? (decimal?)null : price, operationId, true);
            if (orderIds == null || !orderIds.Any())
            {
                return false;
            }
            await _operationRepository.Complete(operationId);
            foreach (var orderId in orderIds)
            {
                await _orderRepository.Add(client.Platform, pair, orderId);
            }
            return true;
        }

        public async Task Buy(IExchangeClient client, string pair, decimal price)
        {
            var currentBalance = await client.GetBaseCurrencyBalance();
            var moneyToSpend = currentBalance / 100m * (decimal)_config[pair].Share;

            var transformResult = _moneyService.Transform(pair,moneyToSpend, price, 0.26m);
            if (transformResult.TargetCurrencyAmount < _config[pair].MinVolume)
            {
                //Insufficient funds
                return;
            }
            _fileService.GatherDetails(pair, FileSessionNames.Buy_Volume, transformResult.TargetCurrencyAmount);
            var operationId = await _operationRepository.Add(client.Platform, "add order", pair, "buy");
            
            var orderIds = await client.Buy(transformResult.TargetCurrencyAmount, pair, _config[pair].IsMarket ? (decimal?)null : price, operationId);

            if (orderIds != null && orderIds.Any())
            {
                await _operationRepository.Complete(operationId);
                foreach (var orderId in orderIds)
                {
                    await _orderRepository.Add(client.Platform, pair, orderId);
                }
            }
        }

        public async Task<bool> Sell(IExchangeClient client, string pair, decimal price)
        {
            var balanceItems = (await _balanceRepository.Get(client.Platform, pair)).Where(x=>!x.IsBorrowed);
            var isNotSold = false;
            var volume = decimal.Zero;
            var profit = decimal.Zero;
            foreach (var balanceItem in balanceItems)
            {
                var boughtPrice = balanceItem.Volume * balanceItem.Price
                    + _moneyService.FeeToPay(pair,balanceItem.Volume, balanceItem.Price, 0.26m)
                    + _moneyService.FeeToPay(pair,balanceItem.Volume, price, 0.26m);
                var sellPrice = balanceItem.Volume * price;

                if (boughtPrice < sellPrice || balanceItem.NotSold >= _config[pair].MaxMissedSells)
                {
                    volume += balanceItem.Volume;
                    profit += sellPrice - boughtPrice;
                }
                else
                {
                    if (balanceItem.NotSoldtDate > _dateTime.Now.AddMinutes(-_config[pair].ThresholdMinutes))
                    {
                        //_fileService.Write(pair, $"Not worths to sell, and too short.");
                    }
                    else
                    {
                        _fileService.GatherDetails(pair, FileSessionNames.Not_Sold_Volume, balanceItem.Volume);
                        await _notSoldRepository.SetNotSold(client.Platform, pair, false);
                    }

                    isNotSold = true;
                }
            }

            if (volume == decimal.Zero)
            {
                return !isNotSold;
            }
            _fileService.GatherDetails(pair, FileSessionNames.Sell_Volume, volume);
            _fileService.GatherDetails(pair, FileSessionNames.Profit, profit);
            var operationId = await _operationRepository.Add(client.Platform, "add order", pair, "sell");

            var orderIds = await client.Sell(volume, pair, _config[pair].IsMarket ? (decimal?)null : price, operationId);
            if (orderIds == null || !orderIds.Any())
            {
                return false;
            }
            await _operationRepository.Complete(operationId);
            foreach (var orderId in orderIds)
            {
                await _orderRepository.Add(client.Platform, pair, orderId);
            }
            return true;

        }
    }
}
