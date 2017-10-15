using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public class OrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IExchangeClient[] _clients;
        private readonly IBalanceRepository _balanceRepository;

        public OrderService(IOrderRepository orderRepository, IExchangeClient[] clients, IBalanceRepository balanceRepository)
        {
            _orderRepository = orderRepository;
            _clients = clients;
            _balanceRepository = balanceRepository;
        }

        public async Task CheckOpenOrders(IExchangeClient client)
        {
            var openOrders = await _orderRepository.Get(client.Platform);
            var statuses = await client.GetOrderStatus(openOrders.Keys.ToArray());

            foreach (var status in statuses.Where(x=>x.Value == OrderStatus.Closed))
            {
                var order = openOrders.FirstOrDefault(x => x.Key == status.Key);
                _balanceRepository.Add(client.Platform, order.Value, order)
            }

        }
    }
}
