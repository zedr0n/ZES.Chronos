using System;
using System.Collections.Concurrent;
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
using ZES.Infrastructure.Branching;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.GraphQl;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Domain;
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
            c.RegisterSingleton<Func<IQueryHandler<AccountStatsQuery, AccountStats>>>(() => c.GetInstance<IQueryHandler<AccountStatsQuery, AccountStats>>);
            c.RegisterSingleton<AssetPoolFactory>();
        }
        
        /// <summary>
        /// Root graphql query for Accounts domain
        /// </summary>
        public class Query : GraphQlQuery
        {
            private readonly IBus _bus;

            /// <summary>
            /// Initializes a new instance of the <see cref="Query"/> class.
            /// </summary>
            /// <param name="bus">ZES Bus</param>
            /// <param name="log">Log service</param>
            public Query(IBus bus, ILog log) 
                : base(bus, log )
            {
                _bus = bus;
            }

            /// <summary>
            /// Account stats GraphQL query
            /// </summary>
            /// <returns>Account stats</returns>
            public Stats AccountStats() => Resolve(new StatsQuery());

            public AccountStats AccountStats(string accountName, Asset denominator = null, Currency currency = null, string date = null, List<AssetQuoteOverride> assetQuoteOverrides = null, bool? immediate = null)
            {
                var time = date?.ToTime();

                if (denominator == null && currency != null)
                    denominator = currency;
                return Resolve(new AccountStatsQuery(accountName, denominator)
                {
                    ConvertToDenominatorAtTxDate = immediate ?? true,
                    Timestamp = time,
                    QueryNet = true,
                    AssetQuoteOverrides = assetQuoteOverrides
                });  
            }

            public AccountStats CombinedAccountStats(string[] accounts, Asset denominator = null, string date = null, List<AssetQuoteOverride> assetQuoteOverrides = null)
            {
                var time = date?.ToTime();
                return Resolve(new CombinedAccountStatsQuery(accounts.ToList(), denominator)
                {
                    Timestamp = time,
                    QueryNet = true,
                    AssetQuoteOverrides = assetQuoteOverrides
                }); 
            }

            public BlendedIrr BlendedIrr(string[] accounts, Asset denominator = null, string date = null, string startDate = null)
            {
                var time = date?.ToTime();
                var startTime = startDate?.ToTime().ToInstant() ?? default;

                return Resolve(new BlendedIrrQuery(accounts.ToList(), denominator)
                {
                    Timestamp = time,
                    QueryNet = true,
                    Start = startTime
                });
            }
            
            public TransactionList TransactionList(string account) => Resolve(new TransactionListQuery(account));
            public List<TransactionInfo> TransactionInfos(string account)
            {
                var txList = _bus.QueryAsync(new TransactionListQuery(account)).Result;
                var list = txList.TxId.Select(tx => _bus.QueryAsync(new TransactionInfoQuery(tx)).Result).ToList();
                return list;
            }
        }

        /// <summary>
        /// Root graphql mutation for Accounts domain
        /// </summary>
        public class Mutation : GraphQlMutation
        {
            private readonly IBus _bus;
            private readonly Query _queries;
            private readonly Core.Config.Query _coreQueries;

            private readonly ConcurrentDictionary<string, Asset> _assets;

            /// <summary>
            /// Root GraphQL mutation class for the Accounts domain.
            /// </summary>
            public Mutation(IBus bus, ILog log, IBranchManager manager, GraphQlResolver resolver)
                : base(bus, log, manager, resolver)
            {
                _bus = bus;
                _queries = new Query(bus, log);
                _coreQueries = new Core.Config.Query(bus, log);
                _assets = new ConcurrentDictionary<string, Asset>();
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
                var time = date?.ToTime();

                if (!Enum.TryParse<AccountType>(type, out var accountType))
                    return false;

                return time != null ? Resolve(new RetroactiveCommand<CreateAccount>(new CreateAccount(name, accountType), time) { Guid = guid }) 
                    : Resolve(new CreateAccount(name, accountType) { Guid = guid }); 
            }

            public bool SpendAsset(string account, double amount, string assetId, string costAssetId, double? cost, string date, string guid)
            {
                var time = date?.ToTime();

                var asset = _assets.GetOrAdd(assetId, x =>
                {
                    var assetsList = Resolve(new AssetPairsInfoQuery() {Timeline = BranchManager.Master}); 
                    var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == x);
                    return asset ?? throw new InvalidOperationException($"Asset {x} not registered");
                });

                Asset costAsset = null;
                if (!string.IsNullOrEmpty(costAssetId))
                {
                    costAsset = _assets.GetOrAdd(costAssetId, x =>
                    {
                        var assetsList = Resolve(new AssetPairsInfoQuery() { Timeline = BranchManager.Master }); 
                        var a = assetsList.Assets.SingleOrDefault(a => a.AssetId == x);
                        return a ?? throw new InvalidOperationException($"Asset {x} not registered");
                    });
                }
                else
                {
                    if(cost != null)
                        throw new InvalidOperationException("Cost asset id is required");
                    
                    var assetPairs = Resolve(new AssetPairsInfoQuery() { Timeline = BranchManager.Master });
                    var pair = assetPairs.GetPairs()
                        .FirstOrDefault(x => x.forAsset.AssetId == asset.AssetId);
                    if (pair != default)
                        costAsset = pair.domAsset;
                }
                var quantity = new Quantity(amount, asset);
                var costQuantity = new Quantity(cost ?? double.NaN, costAsset); 
                
                return time != null ? Resolve(new RetroactiveCommand<SpendAsset>(new SpendAsset(account, quantity, costQuantity), time) { Guid = guid }) 
                    : Resolve(new SpendAsset(account, quantity, costQuantity) { Guid = guid });
            }

            public bool TransactAsset(string account, double amount, string assetId, string costAssetId, double? cost, string date, string guid, double? fee)
            {
                var time = date?.ToTime();

                var asset = _assets.GetOrAdd(assetId, x =>
                {
                    var assetsList = Resolve(new AssetPairsInfoQuery() {Timeline = BranchManager.Master}); 
                    var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == x);
                    return asset ?? throw new InvalidOperationException($"Asset {x} not registered");
                });

                Asset costAsset = null;
                if (!string.IsNullOrEmpty(costAssetId))
                {
                    costAsset = _assets.GetOrAdd(costAssetId, x =>
                    {
                        var assetsList = Resolve(new AssetPairsInfoQuery() { Timeline = BranchManager.Master }); 
                        var a = assetsList.Assets.SingleOrDefault(a => a.AssetId == x);
                        return a ?? throw new InvalidOperationException($"Asset {x} not registered");
                    });
                }
                else
                {
                    if(cost != null)
                        throw new InvalidOperationException("Cost asset id is required");
                    
                    var assetPairs = Resolve(new AssetPairsInfoQuery() { Timeline = BranchManager.Master });
                    var pair = assetPairs.GetPairs()
                        .FirstOrDefault(x => x.forAsset.AssetId == asset.AssetId);
                    if (pair != default)
                        costAsset = pair.domAsset;
                }
                var quantity = new Quantity(amount, asset);
                var costQuantity = new Quantity(cost ?? double.NaN, costAsset); 

                return time != null ? Resolve(new RetroactiveCommand<TransactAsset>(new TransactAsset(account, quantity, costQuantity) { Fee = fee != null ? new Quantity(fee.Value, costAsset) : null } , time) { Guid = guid }) 
                    : Resolve(new TransactAsset(account, quantity, costQuantity) { Guid = guid, Fee = fee != null ? new Quantity(fee.Value, costAsset) : null });
            }
            
            public bool DepositAsset(string name, double amount, string assetId, string date, string guid)
            {
                var time = date?.ToTime();

                var asset = _assets.GetOrAdd(assetId, x =>
                {
                    var assetsList = Resolve(new AssetPairsInfoQuery() { Timeline = BranchManager.Master }); 
                    var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == x);
                    return asset ?? throw new InvalidOperationException($"Asset {x} not registered");
                });
                
                return time != null ? Resolve(new RetroactiveCommand<DepositAsset>(new DepositAsset(name, new Quantity(amount, asset)), time) {Guid = guid}) 
                    : Resolve(new DepositAsset(name, new Quantity(amount, asset)) {Guid = guid});
            }

            public bool CreateTransaction(string txId, string assetId, double amount, string transactionType, string date,string comment, string guid, string relatedAssetId = null, string account = null)
            {
                if (account != null && !Guid.TryParse(txId, out var _))
                    throw new InvalidOperationException("Invalid txId, should be a guid");
                
                if(relatedAssetId == string.Empty)
                    relatedAssetId = null;
                
                var time = date?.ToTime();
                var asset = _assets.GetOrAdd(assetId, x =>
                {
                    var assetsList = Resolve(new AssetPairsInfoQuery() { Timeline = BranchManager.Master }); 
                    var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == x);
                    return asset ?? throw new InvalidOperationException($"Asset {x} not registered");
                });
                
                var res = time != null ? Resolve(new RetroactiveCommand<CreateTransaction>(new CreateTransaction(txId, new Quantity(amount, asset), Enum.Parse<Transaction.TransactionType>(transactionType), comment, relatedAssetId), time) { Guid = account != null ? txId : guid }) :
                    Resolve(new CreateTransaction(txId, new Quantity(amount, asset), Enum.Parse<Transaction.TransactionType>(transactionType), comment, relatedAssetId) { Guid = account != null ? txId : guid });
                
                if(account != null)
                    res &= AddTransaction(account, txId, date, guid); 
                return res;
            }

            public bool AddTransaction(string account, string txId, string date, string guid)
            {
                var time = date?.ToTime();
                return time != null ? Resolve(new RetroactiveCommand<AddTransaction>(new AddTransaction(account, txId), time) { Guid = guid }) :
                    Resolve(new AddTransaction(account, txId) { Guid = guid });
            }

            public bool UpdateQuotes(string account, string denominator, string date = null)
            {
                var time = date?.ToTime();
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
                        if (time == null || assetPairInfo.QuoteDates.All(d => d != time.ToInstant()))
                            Resolve(time != null ? new RetroactiveCommand<UpdateQuote>(new UpdateQuote(fordom), time) : new UpdateQuote(fordom));
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
            
            public bool CreateTransfer(string txId, string fromAccount, string toAccount, string assetId, double amount, double? fee, string feeAssetId = null, string date = null)
            {
                if (!Guid.TryParse(txId, out var _))
                    throw new InvalidOperationException("Invalid txId, should be a guid");
                
                var asset = _assets.GetOrAdd(assetId, x =>
                {
                    var assetsList = Resolve(new AssetPairsInfoQuery() { Timeline = BranchManager.Master }); 
                    var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == x);
                    return asset ?? throw new InvalidOperationException($"Asset {x} not registered");
                });
                
                var feeAsset = feeAssetId == null ? asset : _assets.GetOrAdd(feeAssetId, x =>
                {
                    var assetsList = Resolve(new AssetPairsInfoQuery() { Timeline = BranchManager.Master }); 
                    var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == x);
                    return asset ?? throw new InvalidOperationException($"Asset {x} not registered");
                });
                
                var time = date?.ToTime();

                var feeQuantity = fee != null ? new Quantity(fee.Value, feeAsset) : null;
                
                var result = time != null ? Resolve(new RetroactiveCommand<StartTransfer>(new StartTransfer(txId, fromAccount, toAccount, new Quantity(amount, asset)) { Fee = feeQuantity }, time) { Guid = txId})
                    : Resolve(new StartTransfer(txId, fromAccount, toAccount, new Quantity(amount, asset)) { Guid = txId, Fee = feeQuantity });

                return result;
            }
        }
    }
}