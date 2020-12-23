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

            public TransactionInfo TransactionInfo(string txId, string assetId = null)
            {
                Asset asset = null;
                if (assetId != null)
                {
                    var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                    asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == assetId);
                    if (asset == null)
                        throw new InvalidOperationException();
                }

                var latest = Resolve(new TransactionInfoQuery(txId));
                if (latest == null || assetId == null) 
                    return null;
                
                var date = latest.Date;
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

            public bool RecordTransaction(string txId, double amount, string assetId, string type, string comment, string date)
            {
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == assetId);
                var nDate = InstantPattern.ExtendedIso.Parse(date);
                if (asset == null || !Enum.TryParse<Transaction.TransactionType>(type, out var eType) || !nDate.Success)
                    return false;
                
                return Resolve(new RetroactiveCommand<RecordTransaction>(new RecordTransaction(txId, new Quantity(amount, asset), eType, comment), nDate.Value));
           }
            
            public bool UpdateQuote(string forAsset, string domAsset)
            {
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var forAssetObj = assetsList.Assets.SingleOrDefault(a => a.AssetId == forAsset || a.Ticker == forAsset);
                var domAssetObj = assetsList.Assets.SingleOrDefault(a => a.AssetId == domAsset || a.Ticker == domAsset);
                if (forAssetObj == null || domAssetObj == null)
                    return false;
                
                return Resolve(new UpdateQuote(AssetPair.Fordom(forAssetObj, domAssetObj)));
            }
        }
    }
}