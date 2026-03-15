using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            /// <returns>True if successful</returns>
            public bool CreateAccount(string name, string type, string guid)
            {
                if (!Enum.TryParse<AccountType>(type, out var accountType))
                    return false;
                
                return Resolve(new CreateAccount(name, accountType) { Guid = guid });
            }

            public bool DepositAsset(string name, Asset asset, Currency currency, double amount)
            {
                if(currency != null)
                    asset = currency;
                return Resolve(new DepositAsset(name, new Quantity(amount, asset)));
            }

            public bool AddTransaction(string name, string txId) => Resolve(new AddTransaction(name, txId));

            public bool UpdateQuotes(string account, string denominator)
            {
                var time = _timeline.Now;
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var denominatorAsset = assetsList.Assets.SingleOrDefault(a => a.AssetId == denominator);

                if (denominatorAsset == null)
                    throw new InvalidOperationException($"Asset {denominator} not registered");

                foreach (var asset in assetsList.Assets.Where(a => a.AssetId != denominator))
                {
                    var fordom = AssetPair.Fordom(asset, denominatorAsset);
                    var assetPairInfo = _coreQueries.AssetPairInfo(fordom);
                    if (assetPairInfo.QuoteDates.All(d => d != time.ToInstant()))
                        _bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(fordom), time)).Wait();
                }
                    
                var txList = _queries.TransactionInfos(account); 
                foreach (var t in txList)
                {
                    var fordom = AssetPair.Fordom(t.Quantity.Denominator, denominatorAsset);
                    var assetPairInfo = _coreQueries.AssetPairInfo(fordom);
                    if (!assetPairInfo.QuoteDates.Any(d => d.InUtc().Year == t.Date.InUtc().Year && d.InUtc().Month == t.Date.InUtc().Month && d.InUtc().Day == t.Date.InUtc().Day))
                        _bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(fordom), t.Date.InUtc().LocalDateTime.Date.AtMidnight().InUtc().ToInstant().ToTime())).Wait();
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