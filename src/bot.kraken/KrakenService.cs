using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using bot.kraken.Model;
using bot.model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrderStatus = bot.model.OrderStatus;

namespace bot.kraken
{
    public interface IKrakenClientService : IExchangeClient
    {
        Task<ServerTime> GetServerTime();
        Task<Dictionary<string, Asset>> GetAssetInfo(string assetClass = "currency", params string[] assets);
        Task<Dictionary<string, AssetPair>> GetTradableAssetPairs(params string[] pairs);
        Task<List<OrderInfo>> GetClosedOrders(int? userref = null);
        List<OrderInfo> ToOrders(Dictionary<string, object> orders);
        Task<List<OrderInfo>> GetOrdersInfo(params string[] orderIds);
        Task<Dictionary<string, decimal>> GetBalance();
    }

    public class KrakenClientService : IKrakenClientService
    {
        public string Platform => "kraken";
        
        private readonly Config _config;
        private readonly IKrakenRepository _repository;
        private readonly IKrakenConfig _krakenConfig;

        public KrakenClientService(Config config, IKrakenRepository repository, IKrakenConfig krakenConfig)
        {
            _config = config;
            _repository = repository;
            _krakenConfig = krakenConfig;
        }

        public async Task<ServerTime> GetServerTime()
        {
            return await _repository.CallPublic<ServerTime>("Time");
        }

        public async Task<Dictionary<string, Asset>> GetAssetInfo(string assetClass = "currency", params string[] assets)
        {
            var pairs = new Dictionary<string, string>()
                .AddParam("aclass", assetClass)
                .AddParam("asset", assets);
            return await _repository.CallPublic<Dictionary<string, Asset>>("Assets", pairs);
        }

        public async Task<Dictionary<string, AssetPair>> GetTradableAssetPairs(params string[] pairs)
        {
            var paramPairs = new Dictionary<string, string>()
                .AddParam("pair", pairs);

            return await _repository.CallPublic<Dictionary<string, AssetPair>>("AssetPairs", paramPairs);
        }


        public async Task<SinceResponse<ITrade>> GetTrades(string lastId = null, params string[] pairs)
        {
            var paramPairs = new Dictionary<string, string>()
                .AddParam("since", lastId)
                .AddParam("pair", pairs);

            var response = await _repository.CallPublic<Dictionary<string, object>>("Trades", paramPairs);

            var result = new SinceResponse<ITrade> { Results = new List<ITrade>() };
            if (response == null || !response.Any()) return result;

            result.LastId = Convert.ToString(response.Last().Value);
            if (response.Count <= 1) return result;

            foreach (var tradesPair in response.Take(response.Count - 1))
            {
                var trades = tradesPair.Value as JArray;
                if (trades == null)
                {
                    continue;
                }
                foreach (var arr in trades)
                {
                    result.Results.Add(new BaseTrade
                    {
                        PairName = tradesPair.Key,
                        Price = (decimal)arr[0],
                        Volume = (decimal)arr[1],
                        DateTime = _repository.UnixTimeStampToDateTime((double)arr[2]),
                        TransactionType = arr[3].ToString() == "b" ? TransactionType.Buy : TransactionType.Sell,
                        PriceType = arr[4].ToString() == "m" ? PriceType.Market : PriceType.Limit,
                        Misc = arr[5].ToString()
                    });
                }
            }

            return result;
        }

        public async Task<List<string>> Buy(decimal volume, string pair, decimal? price = null, int? operationId = null, bool isReturn = false)
        {
            var additionalParams = new Dictionary<string, string>();

            if (isReturn && _krakenConfig.PairVariables[KrakenConfig.ShortLaverage] != null)
            {
                additionalParams.Add("laverage", _krakenConfig.PairVariables[KrakenConfig.ShortLaverage].ToString());
            }

            if (!isReturn && _krakenConfig.PairVariables[KrakenConfig.LongLaverage] != null)
            {
                additionalParams.Add("laverage", _krakenConfig.PairVariables[KrakenConfig.LongLaverage].ToString());
            }

            return await AddOrder(OrderType.buy, price.HasValue ? "limit" : "market", volume, pair, price, operationId, additionalParams);
        }

        public async Task<List<string>> Sell(decimal volume, string pair, decimal? price = null, int? operationId = null, bool isBorrow = false)
        {
            var additionalParams = new Dictionary<string, string>();

            if (isBorrow && _krakenConfig.PairVariables[KrakenConfig.ShortLaverage]!=null)
            {
                additionalParams.Add("laverage", _krakenConfig.PairVariables[KrakenConfig.ShortLaverage].ToString());
            }

            if (!isBorrow && _krakenConfig.PairVariables[KrakenConfig.LongLaverage] != null)
            {
                additionalParams.Add("laverage", _krakenConfig.PairVariables[KrakenConfig.LongLaverage].ToString());
            }

            return await AddOrder(OrderType.sell, price.HasValue ? "limit" : "market", volume, pair, price, operationId, additionalParams);
        }

        public async Task<decimal> GetAvailableMargin(string asset)
        {
            var tradeBalance = await GetTradeBalance(asset);
            if (tradeBalance != null && tradeBalance.ContainsKey(TradeBalanceType.FreeMargin))
            {
                return tradeBalance[TradeBalanceType.FreeMargin];
            }
            return decimal.Zero;
        }

        public async Task<List<string>> AddOrder(OrderType operationType,
            string orderType,
            decimal volume,
            string pair, decimal? price = null, int? operationId = null, Dictionary<string, string> additionalParameters = null)
        {
            var pars = new Dictionary<string, string>
            {
                {"pair", pair},
                {"type", operationType.ToString()},
                {"ordertype", orderType},
                {"volume", volume.ToString(CultureInfo.InvariantCulture)}
            };
            if (price.HasValue)
            {
                pars.Add("price", price.ToString());
            }
            if (operationId.HasValue)
            {
                pars.Add("userref", operationId.ToString());
            }

            if (additionalParameters != null)
            {
                foreach (var parameter in additionalParameters)
                {
                    if (pars.ContainsKey(parameter.Key))
                    {
                        pars[parameter.Key] = parameter.Value;
                    }
                    else
                    {
                        pars.Add(parameter.Key, parameter.Value);
                    }
                }
            }

            var response = await _repository.CallPrivate<Dictionary<string, object>>("AddOrder", pars);

            var txid = response["txid"];
            return JsonConvert.DeserializeObject<List<string>>(txid.ToString());

        }

        public async Task<List<string>> AddOrder(OrderType orderType, decimal volume, string pair, decimal? price = null, int? operationId = null)
        {
            var pars = new Dictionary<string, string>
            {
                {"pair", pair},
                {"type", orderType.ToString().Replace("_","-")},
                {"ordertype", price.HasValue ? "limit" : "market"},
                {"volume", volume.ToString(CultureInfo.InvariantCulture)}
            };
            if (price.HasValue)
            {
                pars.Add("price", price.ToString());
            }
            if (operationId.HasValue)
            {
                pars.Add("userref", operationId.ToString());
            }

            var response = await _repository.CallPrivate<Dictionary<string, object>>("AddOrder", pars);

            var txid = response["txid"];
            return JsonConvert.DeserializeObject<List<string>>(txid.ToString());

        }

        public List<OrderInfo> ToOrders(Dictionary<string, object> orders)
        {
            var result = new List<OrderInfo>();

            foreach (var order in orders)
            {
                var details = JsonConvert.DeserializeObject<Dictionary<string, object>>(order.Value.ToString());
                var item = new OrderInfo();
                item.Id = order.Key;
                if (details.ContainsKey("descr"))
                {
                    var desc = JsonConvert.DeserializeObject<Dictionary<string, object>>(details["descr"].ToString());
                    Set("pair", desc, o => item.Pair = Convert.ToString(o));
                    Set("type", desc, o => item.OrderType = Convert.ToString(o).ToEnum<OrderType>());
                    Set("ordertype", desc, o => item.OrderPriceType = Convert.ToString(o).ToEnum<OrderPriceType>());
                    Set("price", desc, o => item.PrimaryPrice = Convert.ToDecimal(o));
                    Set("price2", desc, o => item.SecondaryPrice = Convert.ToDecimal(o));
                    Set("leverage", desc, o => item.Leverage = Convert.ToString(o));
                }

                Set("status", details, o => item.Status = Convert.ToString(o).ToEnum<KrakenOrderStatus>());
                Set("reason", details, o => item.Reason = Convert.ToString(o));
                Set("userref", details, o => item.UserRef = Convert.ToString(o));

                Set("vol", details, o => item.Volume = Convert.ToDecimal(o));
                Set("vol_exec", details, o => item.VolumeExec = Convert.ToDecimal(o));
                Set("cost", details, o => item.Cost = Convert.ToDecimal(o));
                Set("price", details, o => item.Price = Convert.ToDecimal(o));
                Set("fee", details, o => item.Fee = Convert.ToDecimal(o));
                Set("misc", details, o => item.Misc = Convert.ToString(o));

                result.Add(item);
            }
            return result;
        }

        private void Set(string name, Dictionary<string, object> details, Action<object> setAction)
        {
            if (details.ContainsKey(name))
            {
                setAction(details[name]);
            }
        }

        public async Task<List<OrderInfo>> GetOrdersInfo(params string[] orderIds)
        {
            var paramPairs = new Dictionary<string, string>()
            {
                {"txid", string.Join(",", orderIds) }
            };
            var response = await _repository.CallPrivate<Dictionary<string, object>>("QueryOrders", paramPairs);
            return ToOrders(response);
        }

        public async Task<List<string>> GetOrdersIds(int userref)
        {
            var result = new List<string>();
            result.AddRange((await GetOpenOrders(userref)).Select(x => x.Id));
            result.AddRange((await GetClosedOrders(userref)).Select(x => x.Id));
            return result;

        }

        public async Task<List<OrderInfo>> GetClosedOrders(int? userref = null)
        {
            var paramPairs = new Dictionary<string, string>();

            if (userref.HasValue)
            {
                paramPairs.Add("userref", userref.ToString());
            }

            var response = await _repository.CallPrivate<Dictionary<string, object>>("ClosedOrders", paramPairs);

            var result = new List<OrderInfo>();
            if (response.Count <= 1)
            {
                return new List<OrderInfo>();

            }
            foreach (var closed in response.Take(response.Count - 1))
            {
                var orders = JsonConvert.DeserializeObject<Dictionary<string, object>>(closed.Value.ToString());
                result.AddRange(ToOrders(orders));
            }
            return result;
        }


        public async Task<List<Order>> GetOpenOrders(int? userref = null)
        {
            var paramPairs = new Dictionary<string, string>();

            if (userref.HasValue)
            {
                paramPairs.Add("userref", userref.ToString());
            }
            var response = await _repository.CallPrivate<Dictionary<string, object>>("OpenOrders", paramPairs);
            if (response.Count <= 1)
            {
                return new List<Order>();

            }
            var orderInfos = ToOrders(response);
            return orderInfos.Select(x => new Order
            {
                Id = x.Id,
                OrderStatus = x.Status == KrakenOrderStatus.closed ? OrderStatus.Closed : OrderStatus.Pending,
                Volume = x.Volume,
                Price = x.Price,
                Pair = x.Pair,
                OrderType = x.OrderType
            }).ToList();
        }

        public async Task<List<Order>> GetOrders(params string[] refs)
        {
            var orderInfos = await GetOrdersInfo(refs);
            return orderInfos.Select(x => new Order
            {
                Id = x.Id,
                OrderStatus = MapStatus(x.Status),
                Volume = x.Volume,
                Price = x.Price,
                Pair = x.Pair,
                OrderType = x.OrderType,
                IsBorrowed = x.Leverage == _krakenConfig.PairVariables["short laverage"].ToString()
            }).ToList();
        }

        private OrderStatus MapStatus(KrakenOrderStatus krakenOrderStatus)
        {
            switch (krakenOrderStatus)
            {

                case KrakenOrderStatus.closed:
                    return OrderStatus.Closed;
                case KrakenOrderStatus.canceled:
                    return OrderStatus.Cancelled;
                case KrakenOrderStatus.pending:
                case KrakenOrderStatus.open:
                case KrakenOrderStatus.expired:
                default:
                    return OrderStatus.Pending;
            }
        }
        

        public async Task<decimal> GetBaseCurrencyBalance()
        {
            var balance = await GetBalance();
            if (balance.ContainsKey(_config.BaseCurrency))
            {
                return balance[_config.BaseCurrency];
            }
            return decimal.Zero;
        }

        public async Task<Dictionary<string, decimal>> GetBalance()
        {
            return await _repository.CallPrivate<Dictionary<string, decimal>>("Balance");
        }

        public async Task<Dictionary<TradeBalanceType, decimal>> GetTradeBalance(string asset)
        {
            var dict = new Dictionary<string, TradeBalanceType>()
            {
                {"eb", TradeBalanceType.EquivalentBalance },
                {"tb", TradeBalanceType.TradeBalance },
                {"m", TradeBalanceType.Margin },
                {"n", TradeBalanceType.UnrealizedProfitLoss },
                {"c", TradeBalanceType.Cost },
                {"v", TradeBalanceType.Valuation },
                {"e", TradeBalanceType.Equity },
                {"mf", TradeBalanceType.FreeMargin },
                {"ml", TradeBalanceType.MarginLevel }
            };

            var tradebalance = await _repository.CallPrivate<Dictionary<string, decimal>>("TradeBalance");
            return tradebalance.Where(x => dict.ContainsKey(x.Key)).ToDictionary(x => dict[x.Key], x => x.Value);
        }

        
    }

    public class InternalException : Exception
    {
        public InternalException(string message) : base(message)
        {

        }

    }

}


