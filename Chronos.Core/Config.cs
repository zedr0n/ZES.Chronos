using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Chronos.Core.Commands;
using Chronos.Core.Net;
using Chronos.Core.Queries;
using SimpleInjector;
using ZES.Infrastructure;
using ZES.Infrastructure.Branching;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.GraphQl;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.GraphQL;
using ZES.Interfaces.Infrastructure;
using ZES.Utils;

#pragma warning disable SA1600

namespace Chronos.Core
{
    /// <inheritdoc />
    public class ConfigurationOverride : IConfigurationOverride
    {
        /// <inheritdoc />
        public void ApplyOverride()
        {
        }
    }    
    
    /// <summary>
    /// Core config
    /// </summary>
    public class Config
    {
        public static void RegisterOverrides(Container c)
        {
            c.RegisterConfigurationOverrides([Assembly.GetExecutingAssembly()]);    
        }        
        
        [Registration]
        public static void RegisterAll(Container c)
        {
            c.RegisterAll(Assembly.GetExecutingAssembly());
            c.RegisterSingleton<IUpdateCommandFactory, UpdateCommandFactory>();
            c.RegisterSingleton<IWebApiProvider, WebApiProvider>();
            c.Collection.Register<IWebQuoteApi>(
                typeof(CoinEodQuoteApi),
                typeof(FxEodQuoteApi),
                typeof(EquityEodQuoteApi),
                typeof(CoinIntradayQuoteApi),
                typeof(FxIntradayQuoteApi),
                typeof(EquityIntradayQuoteApi));
            c.RegisterSingleton<IWebSearchApi, WebSearchApi>();
            c.Collection.Register<IWebSearchApi>(typeof(WebSearchApi));
        }

        /// <inheritdoc />
        public class Query : GraphQlQuery
        {
            private readonly IBus _bus;
            private readonly ConcurrentDictionary<string, Asset> _assets = new();

            public Query(IBus bus, ILog log)
                : base(bus, log)
            {
                _bus = bus;
            }
            
            public Currency Currency { get; }

            public AssetPairInfo AssetPairInfo(string fordom) => Resolve(new AssetPairInfoQuery(fordom));

            public AssetQuote AssetQuote(string forAssetId, string domAssetId, string date = null)
            {
                var nDate = date?.ToTime(); 
                
                var forAsset = _assets.GetOrAdd(forAssetId, x =>
                {
                    var assetsList = Resolve(new AssetPairsInfoQuery() { Timeline = BranchManager.Master }); 
                    var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == x);
                    return asset ?? throw new InvalidOperationException($"Asset {x} not registered");
                });
                var domAsset = _assets.GetOrAdd(domAssetId, x =>
                {
                    var assetsList = Resolve(new AssetPairsInfoQuery() { Timeline = BranchManager.Master }); 
                    var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == x);
                    return asset ?? throw new InvalidOperationException($"Asset {x} not registered");
                });
                
                return Resolve(new HistoricalQuery<AssetQuoteQuery, AssetQuote>(new AssetQuoteQuery(forAsset, domAsset) { UpdateQuote = true }, nDate));
            }
            
            /// <summary>
            /// Gets the transaction info
            /// </summary>
            /// <param name="txId">Transaction id</param>
            /// <param name="assetId">Denominator asset</param>
            /// <returns>Transaction info</returns>
            /// <exception cref="InvalidOperationException">Denominator asset not registered</exception>
            public TransactionInfo TransactionInfo(string txId, string assetId = null)
            {
                Asset asset = null;
                if (assetId != null)
                {
                    var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                    asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == assetId);
                    if (asset == null)
                        throw new InvalidOperationException($"Asset {assetId} not registered");
                }

                var txInfo = Resolve(new TransactionInfoQuery(txId));
                var date = txInfo.Date.ToTime();
                if (date == null)
                    throw new InvalidOperationException($"No transaction information found for {txId}");
                
                var quote = txInfo.Quotes.SingleOrDefault(q => q.Denominator == asset);
                if (quote != null || assetId == null)
                    return new TransactionInfo(txInfo.TxId, date.ToInstant(), quote ?? txInfo.Quantity, txInfo.TransactionType, txInfo.Comment, txInfo.AssetId);
                
                _bus.Command(new RetroactiveCommand<UpdateQuote>(
                    new UpdateQuote(AssetPair.Fordom(txInfo.Quantity.Denominator, asset)), date)).Wait();

                return Resolve(
                    new HistoricalQuery<TransactionInfoQuery, TransactionInfo>(
                        new TransactionInfoQuery(txId, asset), date));
            }
        }

        /// <inheritdoc />
        public class Mutation : GraphQlMutation
        {
            private readonly IBus _bus;
            
            public Mutation(IBus bus, ILog log, IBranchManager manager, GraphQlResolver resolver)
                : base(bus, log, manager, resolver)
            {
                _bus = bus;
            }

            public bool RegisterCurrencyPair(string forCcy, string domCcy, string guid)
            {
                var forAsset = new Currency(forCcy);
                var domAsset = new Currency(domCcy);
                var fordom = AssetPair.Fordom(forAsset, domAsset);
                var command = new RegisterAssetPair(fordom, forAsset, domAsset) { Guid = guid };
                var result = Resolve(new RetroactiveCommand<RegisterAssetPair>(command, Time.MinValue));
                return result;
            }

            public bool RegisterAssetPair(Asset forAsset, Asset domAsset, string guid, bool supportsIntraday = true)
            {
                var fordom = AssetPair.Fordom(forAsset, domAsset);
                var command = new RegisterAssetPair(fordom, forAsset, domAsset, supportsIntraday);
                var result = Resolve(new RetroactiveCommand<RegisterAssetPair>(command, Time.MinValue) { Guid = guid });
                return result;
            }

            public bool AddQuoteTicker(string assetId, string ticker, string date, string domAssetId, string guid)
            {
                var time = date?.ToTime() ?? Time.MinValue;
                var fordoms = new List<string>();
                if (domAssetId == null)
                {
                    var assetsList = Resolve(new AssetPairsInfoQuery() { Timeline = BranchManager.Master });
                    fordoms = assetsList.Pairs.Where(x => x.StartsWith(assetId, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    fordoms = new List<string>() { AssetPair.Fordom(assetId, domAssetId) };
                }

                var valid = true;
                foreach (var fordom in fordoms)
                    valid &= Resolve(new RetroactiveCommand<AddQuoteTicker>(new AddQuoteTicker(fordom, ticker), time) { Guid = guid });

                return valid;
            }

            public bool AddStockSplit(string assetId, double ratio, string date, string domAssetId, string guid)
            {
                var time = date?.ToTime();
                var fordoms = new List<string>();
                if (domAssetId == null)
                {
                    var assetsList = Resolve(new AssetPairsInfoQuery() { Timeline = BranchManager.Master });
                    fordoms = assetsList.Pairs.Where(x => x.StartsWith(assetId, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    fordoms = new List<string>() { AssetPair.Fordom(assetId, domAssetId) };
                }

                var valid = true;
                foreach (var fordom in fordoms)
                {
                    valid &= time != null ? Resolve(new RetroactiveCommand<AddStockSplit>(new AddStockSplit(fordom, ratio), time) { Guid = guid }) 
                        : Resolve(new AddStockSplit(fordom, ratio) { Guid = guid });
                }

                return valid;
            }
            
            public bool UpdateQuote(string forAsset, string domAsset, string date = null)
            {
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var forAssetObj = assetsList.Assets.SingleOrDefault(a => a.AssetId == forAsset);
                var domAssetObj = assetsList.Assets.SingleOrDefault(a => a.AssetId == domAsset);
                var nDate = date.ToTime();

                if (forAssetObj == null)
                    throw new InvalidOperationException($"Asset {forAsset} not registered");
                if (domAssetObj == null)
                    throw new InvalidOperationException($"Asset {domAsset} not registered");

                if (!assetsList.Pairs.Contains(AssetPair.Fordom(forAssetObj, domAssetObj)))
                    Resolve(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(forAssetObj, domAssetObj), forAssetObj, domAssetObj), nDate));

                return Resolve(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(forAssetObj, domAssetObj)), nDate));
            }

            public bool AddTransactionQuote(string txId, string assetId, double amount)
            {
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == assetId);

                if (asset == null)
                    throw new InvalidOperationException($"Asset {assetId} not registered");

                var date = _bus.QueryAsync(new TransactionInfoQuery(txId)).Result?.Date.ToTime();
                if (date == null || date == default)
                    throw new InvalidOperationException($"No transaction {txId} found");
                
                return Resolve(new RetroactiveCommand<AddTransactionQuote>(new AddTransactionQuote(txId, new Quantity(amount, asset)), date));
            }
        }
    }
}