using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Chronos.Accounts.Commands;
using Chronos.Accounts.Queries;
using Chronos.Core;
using Chronos.Core.Queries;
using NodaTime;
using NodaTime.Text;
using SimpleInjector;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.GraphQl;
using ZES.Interfaces;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Pipes;
using ZES.Utils;

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
            
            /// <summary>
            /// Initializes a new instance of the <see cref="Query"/> class.
            /// </summary>
            /// <param name="bus">ZES Bus</param>
            public Query(IBus bus) 
                : base(bus)
            {
                _bus = bus;
            }

            /// <summary>
            /// Account stats GraphQL query
            /// </summary>
            /// <returns>Account stats</returns>
            public Stats Stats() => Resolve(new StatsQuery());

            public AccountStats AccountStats(string accountName, string assetId)
            {
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == assetId);
                if (assetId == null)
                    return null;
                return Resolve(new AccountStatsQuery(accountName, asset)); 
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
            private readonly IBranchManager _manager;
            private readonly IBus _bus;

            /// <summary>
            /// Initializes a new instance of the <see cref="Mutation"/> class.
            /// </summary>
            /// <param name="bus">Bus service</param>
            /// <param name="log">Log service</param>
            /// <param name="manager">Branch manager</param>
            public Mutation(IBus bus, ILog log, IBranchManager manager) 
                : base(bus, log)
            {
                _bus = bus;
                _manager = manager;
            }

            /// <summary>
            /// Create the account
            /// </summary>
            /// <param name="name">Account name</param>
            /// <param name="type">Account type</param>
            /// <returns>True if successful</returns>
            public bool CreateAccount(string name, string type)
            {
                if (!Enum.TryParse<AccountType>(type, out var accountType))
                    return false;
                
                return Resolve(new CreateAccount(name, accountType));
            }

            public bool AddTransaction(string name, string txId) => Resolve(new AddTransaction(name, txId));

            public bool AddTransfer(string txId, string fromAccount, string toAccount, string assetId, double amount, string date = null)
            {
                if (date == null)
                    date = string.Empty;
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == assetId);
                var nDate = InstantPattern.ExtendedIso.Parse(date);
                if (asset == null || (!nDate.Success && date != string.Empty ))
                    return false;

                var command = new StartTransfer(txId, fromAccount, toAccount, new Quantity(amount, asset));
                var result = false;
                if (nDate.Success)
                    result = Resolve(new RetroactiveCommand<StartTransfer>(command, nDate.Value));
                else
                    result = Resolve(command);
                _manager.Ready.Wait();

                return result;
            }
        }
    }
}