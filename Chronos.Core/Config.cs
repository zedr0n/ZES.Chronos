using System;
using System.Linq;
using System.Reflection;
using Chronos.Core.Commands;
using Chronos.Core.Queries;
using NodaTime;
using NodaTime.Text;
using SimpleInjector;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.GraphQl;
using ZES.Infrastructure.Utils;
using ZES.Interfaces;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Pipes;
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
        }

        /// <inheritdoc />
        public class Query : GraphQlQuery
        {
            private readonly IBus _bus;
            private readonly ITimeline _timeline;

            public Query(IBus bus, ITimeline timeline)
                : base(bus)
            {
                _bus = bus;
                _timeline = timeline;
            }

            public AssetPairInfo AssetPairInfo(string fordom) => Resolve(new AssetPairInfoQuery(fordom));

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

                var latest = Resolve(new TransactionInfoQuery(txId));
                var date = latest.Date.ToTime();
                if (date == default)
                    throw new InvalidOperationException($"No transaction information found for {txId}");
                
                var quote = latest.Quotes.SingleOrDefault(q => q.Denominator == asset);
                if (quote != null || assetId == null)
                    return new TransactionInfo(latest.TxId, date.ToInstant(), quote ?? latest.Quantity, latest.TransactionType, latest.Comment);
                
                var diff = _timeline.Now - date;
                if (diff.Days > 0 || diff.Hours > 0)
                {
                    _bus.Command(new RetroactiveCommand<UpdateQuote>(
                        new UpdateQuote(AssetPair.Fordom(latest.Quantity.Denominator, asset)), date)).Wait();

                    return Resolve(
                        new HistoricalQuery<TransactionInfoQuery, TransactionInfo>(
                            new TransactionInfoQuery(txId, asset), date));
                }

                _bus.Command(new UpdateQuote(AssetPair.Fordom(latest.Quantity.Denominator, asset))).Wait();
                return Resolve(new TransactionInfoQuery(txId, asset));
            }
        }

        /// <inheritdoc />
        public class Mutation : GraphQlMutation
        {
            private readonly IBus _bus;
            
            public Mutation(IBus bus, ILog log, IBranchManager manager)
                : base(bus, log, manager)
            {
                _bus = bus;
            }

            public bool RegisterCurrencyPair(string forCcy, string domCcy)
            {
                var forAsset = new Currency(forCcy);
                var domAsset = new Currency(domCcy);
                var fordom = AssetPair.Fordom(forAsset, domAsset);
                var command = new RegisterAssetPair(fordom, forAsset, domAsset);
                var result = Resolve(new RetroactiveCommand<RegisterAssetPair>(command, Time.MinValue));
                return result;
            }

            public bool RecordTransaction(string txId, double amount, string assetId, string type, string comment, string date = null)
            {
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == assetId);
                var nDate = date?.ToTime() ?? Time.Default;
                if (asset == null)
                    throw new InvalidOperationException($"Asset {assetId} not registered");
                if (!Enum.TryParse<Transaction.TransactionType>(type, out var eType))
                    throw new ArgumentException("Not a valid transaction type", nameof(type));
                
                return Resolve(new RetroactiveCommand<RecordTransaction>(new RecordTransaction(txId, new Quantity(amount, asset), eType, comment), nDate));
            }
            
            public bool UpdateQuote(string forAsset, string domAsset, string date = null)
            {
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var forAssetObj = assetsList.Assets.SingleOrDefault(a => a.AssetId == forAsset || a.Ticker == forAsset);
                var domAssetObj = assetsList.Assets.SingleOrDefault(a => a.AssetId == domAsset || a.Ticker == domAsset);
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