using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Chronos.Accounts.Commands;
using Chronos.Accounts.Queries;
using Chronos.Core;
using Chronos.Core.Commands;
using Chronos.Core.Queries;
using SimpleInjector;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.GraphQl;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Infrastructure;
using ZES.Utils;

#pragma warning disable SA1600

namespace Chronos.Accounts
{
    /// <summary>
    /// Config for Accounts domain
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// Register all services
        /// </summary>
        /// <param name="c">Container</param>
        [Registration]
        public static void RegisterAll(Container c)
        {
            c.RegisterAll(Assembly.GetExecutingAssembly());
        }
        
        /// <summary>
        /// Root graphql query for Accounts domain
        /// </summary>
        public class Query : GraphQlQuery
        {
            private readonly IBus _bus;
            private readonly ITimeline _timeline;

            /// <summary>
            /// Initializes a new instance of the <see cref="Query"/> class.
            /// </summary>
            /// <param name="bus">ZES Bus</param>
            /// <param name="log">Log service</param>
            /// <param name="timeline"></param>
            public Query(IBus bus, ILog log, ITimeline timeline) 
                : base(bus, log )
            {
                _bus = bus;
                _timeline = timeline;
            }

            /// <summary>
            /// Account stats GraphQL query
            /// </summary>
            /// <returns>Account stats</returns>
            public Stats AccountStats() => Resolve(new StatsQuery());

            public AccountStats AccountStats(string accountName, Asset denominator = null, Currency currency = null, string date = null, bool? immediate = null)
            {
                var nDate = date.ToTime();
                if (nDate == null)
                    nDate = _timeline.Now;

                if (denominator == null && currency != null)
                    denominator = currency;
                return Resolve(new AccountStatsQuery(accountName, denominator)
                {
                    ConvertToDenominatorAtTxDate = immediate ?? false,
                    Timestamp = nDate,
                    QueryNet = true
                });  
            } 
            
            public TransactionList TransactionList(string account) => Resolve(new TransactionListQuery(account));
            public List<TransactionInfo> TransactionInfos(string account)
            {
                var txIds = _bus.QueryAsync(new TransactionListQuery(account)).Result;
                var list = txIds.TxId.Select(tx => _bus.QueryAsync(new TransactionInfoQuery(tx)).Result).ToList();
                return list;
            }
        }

        /// <summary>
        /// Root graphql mutation for Accounts domain
        /// </summary>
        public class Mutation : GraphQlMutation
        {
            private readonly IBus _bus;
            private readonly ITimeline _timeline;
            private readonly Query _queries;
            private readonly Core.Config.Query _coreQueries;

            /// <summary>
            /// Initializes a new instance of the <see cref="Mutation"/> class.
            /// </summary>
            /// <param name="bus">Bus service</param>
            /// <param name="log">Log service</param>
            /// <param name="manager">Branch manager</param>
            /// <param name="timeline">Timeline</param>
            public Mutation(IBus bus, ILog log, IBranchManager manager, ITimeline timeline) 
                : base(bus, log, manager)
            {
                _bus = bus;
                _timeline = timeline;
                _queries = new Query(bus, log, timeline);
                _coreQueries = new Core.Config.Query(bus, timeline, log);
            }

            /// <summary>
            /// Create the account
            /// </summary>
            /// <param name="name">Account name</param>
            /// <param name="type">Account type</param>
            /// <param name="guid">Command guid</param>
            /// <param name="date">Command date</param>
            /// <returns>True if successful</returns>
            public bool CreateAccount(string name, string type, string date, string guid)
            {
                var isRetroactive = date.ToTime() != null && _timeline.Now.ToInstant().Minus(date.ToTime().ToInstant()).TotalSeconds > 60;
                var time = date?.ToTime() ?? _timeline.Now;

                if (!Enum.TryParse<AccountType>(type, out var accountType))
                    return false;

                return isRetroactive ? Resolve(new RetroactiveCommand<CreateAccount>(new CreateAccount(name, accountType), time) { Guid = guid }) 
                    : Resolve(new CreateAccount(name, accountType) { Guid = guid }); 
            }

            public bool TransactAsset(string account, Quantity asset, string costAssetId, double? cost, string date, string guid)
            {
                var isRetroactive = date.ToTime() != null &&
                                    _timeline.Now.ToInstant().Minus(date.ToTime().ToInstant()).TotalSeconds > 60;
                var time = date?.ToTime() ?? _timeline.Now;
                var costAsset = new Asset(costAssetId, AssetType.Currency);
                if (costAssetId == null)
                {
                    if(cost != null)
                        throw new InvalidOperationException("Cost asset id is required");
                    
                    var assetPairs = Resolve(new AssetPairsInfoQuery());
                    var pair = assetPairs.GetPairs()
                        .FirstOrDefault(x => x.forAsset.AssetId == asset.Denominator.AssetId);
                    if (pair != default)
                        costAsset = pair.domAsset;
                }
                var costQuantity = new Quantity(cost ?? double.NaN, costAsset); 

                return isRetroactive
                    ? Resolve(new RetroactiveCommand<TransactAsset>(new TransactAsset(account, asset, costQuantity), time) { Guid = guid }) 
                    : Resolve(new TransactAsset(account, asset, costQuantity) { Guid = guid});
            }
            
            public bool DepositAsset(string name, Asset asset, Currency currency, double amount, string date, string guid)
            {
                var isRetroactive = date.ToTime() != null && _timeline.Now.ToInstant().Minus(date.ToTime().ToInstant()).TotalSeconds > 60;
                var time = date?.ToTime() ?? _timeline.Now;
                
                if(currency != null)
                    asset = currency;
                return isRetroactive ? Resolve(new RetroactiveCommand<DepositAsset>(new DepositAsset(name, new Quantity(amount, asset)), time) {Guid = guid}) 
                    : Resolve(new DepositAsset(name, new Quantity(amount, asset)) {Guid = guid});
            }

            public bool CreateTransaction(string txId, string assetId, double amount, string transactionType, string date,string comment, string guid)
            {
                var isRetroactive = date.ToTime() != null && _timeline.Now.ToInstant().Minus(date.ToTime().ToInstant()).TotalSeconds > 60;
                var time = date?.ToTime() ?? _timeline.Now;
                var assetsList = Resolve(new AssetPairsInfoQuery()); 
                var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == assetId);
                if (asset == null)
                    throw new InvalidOperationException($"Asset {assetId} not registered");
                
                return isRetroactive ? Resolve(new RetroactiveCommand<CreateTransaction>(new CreateTransaction(txId, new Quantity(amount, asset), Enum.Parse<Transaction.TransactionType>(transactionType), comment), time) {Guid = guid}) :
                    Resolve(new CreateTransaction(txId, new Quantity(amount, asset), Enum.Parse<Transaction.TransactionType>(transactionType), comment) {Guid = guid});
            }

            public bool AddTransaction(string account, string txId, string date, string guid)
            {
                var isRetroactive = date?.ToTime() != null && _timeline.Now.ToInstant().Minus(date.ToTime().ToInstant()).TotalSeconds > 60;
                var time = date?.ToTime() ?? _timeline.Now;
                return isRetroactive ? Resolve(new RetroactiveCommand<AddTransaction>(new AddTransaction(account, txId), time) { Guid = guid }) :
                    Resolve(new AddTransaction(account, txId) { Guid = guid });
            }

            public bool UpdateQuotes(string account, string denominator, string date = null)
            {
                var isRetroactive = date.ToTime() != null && _timeline.Now.ToInstant().Minus(date.ToTime().ToInstant()).TotalSeconds > 60;
                var time = date?.ToTime() ?? _timeline.Now;
                var assetsList = Resolve(new AssetPairsInfoQuery()); 
                var denominatorAsset = assetsList.Assets.SingleOrDefault(a => a.AssetId == denominator);

                if (denominatorAsset == null)
                    throw new InvalidOperationException($"Asset {denominator} not registered");
                
                var tasks = new List<Task<bool>>{};
                foreach (var asset in assetsList.Assets.Where(a => a.AssetId != denominator))
                {
                    var precedents = assetsList.GetPrecedents(asset, denominatorAsset);
                    if(precedents == null)
                        continue;

                    foreach (var (forAsset, domAsset) in precedents)
                    {
                        var fordom = AssetPair.Fordom(forAsset, domAsset);
                        var assetPairInfo = _coreQueries.AssetPairInfo(fordom);
                        if (assetPairInfo.QuoteDates.All(d => d != time.ToInstant()))
                            Resolve(isRetroactive ? new RetroactiveCommand<UpdateQuote>(new UpdateQuote(fordom), time) : new UpdateQuote(fordom));
                    }
                }
                    
                var txList = _queries.TransactionInfos(account); 
                foreach (var t in txList)
                {
                    var fordom = AssetPair.Fordom(t.Quantity.Denominator, denominatorAsset);
                    var assetPairInfo = _coreQueries.AssetPairInfo(fordom);
                    if (!assetPairInfo.QuoteDates.Any(d => d.InUtc().Year == t.Date.InUtc().Year && d.InUtc().Month == t.Date.InUtc().Month && d.InUtc().Day == t.Date.InUtc().Day))
                         Resolve(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(fordom), t.Date.InUtc().LocalDateTime.Date.AtMidnight().InUtc().ToInstant().ToTime()));
                }

                return true;
            }
            
            public bool AddTransfer(string txId, string fromAccount, string toAccount, string assetId, double amount, string date = null)
            {
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == assetId);
                var nDate = date.ToTime();
                if (asset == null)
                    throw new InvalidOperationException($"Asset {assetId} not registered");

                var command = new StartTransfer(txId, fromAccount, toAccount, new Quantity(amount, asset));
                var result = Resolve(new RetroactiveCommand<StartTransfer>(command, nDate));

                return result;
            }
        }
    }
}