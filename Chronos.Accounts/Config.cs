using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Chronos.Accounts.Commands;
using Chronos.Accounts.Queries;
using Chronos.Core.Queries;
using SimpleInjector;
using ZES.Infrastructure;
using ZES.Infrastructure.GraphQl;
using ZES.Interfaces;
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
        /// Root graphql query for Accounts damain
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
            /// <summary>
            /// Initializes a new instance of the <see cref="Mutation"/> class.
            /// </summary>
            /// <param name="bus">Bus service</param>
            /// <param name="log">Log service</param>
            public Mutation(IBus bus, ILog log) 
                : base(bus, log)
            {
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
        }
    }
}