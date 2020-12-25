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
using ZES.Interfaces.Pipes;
using ZES.Utils;

namespace Chronos.Core
{
    public class Config
    {
        [Registration]
        public static void RegisterAll(Container c)
        {
            c.RegisterAll(Assembly.GetExecutingAssembly());
        }
        
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
                        return null;
                }

                var latest = Resolve(new TransactionInfoQuery(txId));
                var date = latest.Date;
                if (date == default)
                    return null;
                var quote = latest.Quotes.SingleOrDefault(q => q.Denominator == asset);
                if (quote != null || assetId == null)
                    return new TransactionInfo(latest.TxId, date, quote ?? latest.Quantity, latest.TransactionType, latest.Comment);
                
                var diff = _timeline.Now.Minus(date);
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

        public class Mutation : GraphQlMutation
        {
            private readonly IBus _bus;
            
            public Mutation(IBus bus, ILog log)
                : base(bus, log)
            {
                _bus = bus;
            }

            public bool RecordTransaction(string txId, double amount, string assetId, string type, string comment, string date = null)
            {
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == assetId);
                var nDate = date.ToInstant(); 
                if (asset == null || !Enum.TryParse<Transaction.TransactionType>(type, out var eType) || !nDate.Success)
                    return false;
                
                return Resolve(new RetroactiveCommand<RecordTransaction>(new RecordTransaction(txId, new Quantity(amount, asset), eType, comment), nDate.Value));
            }
            
            public bool UpdateQuote(string forAsset, string domAsset, string date = null)
            {
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var forAssetObj = assetsList.Assets.SingleOrDefault(a => a.AssetId == forAsset || a.Ticker == forAsset);
                var domAssetObj = assetsList.Assets.SingleOrDefault(a => a.AssetId == domAsset || a.Ticker == domAsset);
                var nDate = date.ToInstant();

                if (forAssetObj == null || domAssetObj == null || !nDate.Success)
                    return false;

                if (!assetsList.Pairs.Contains(AssetPair.Fordom(forAssetObj, domAssetObj)))
                    Resolve(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(forAssetObj, domAssetObj), forAssetObj, domAssetObj), nDate.Value));

                return Resolve(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(forAssetObj, domAssetObj)), nDate.Value));
            }

            public bool AddTransactionQuote(string txId, string assetId, double amount)
            {
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == assetId);

                if (asset == null)
                    return false;

                var date = _bus.QueryAsync(new TransactionInfoQuery(txId)).Result.Date;
                if (date == default)
                    return false;
                
                return Resolve(new RetroactiveCommand<AddTransactionQuote>(new AddTransactionQuote(txId, new Quantity(amount, asset)), date));
            }
        }
    }
}