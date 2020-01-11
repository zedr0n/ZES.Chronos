using System;
using System.Reflection;
using Chronos.Accounts.Commands;
using SimpleInjector;
using ZES.Infrastructure.Attributes;
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
            /// <summary>
            /// Initializes a new instance of the <see cref="Query"/> class.
            /// </summary>
            /// <param name="bus">ZES Bus</param>
            public Query(IBus bus) 
                : base(bus)
            {
            }
            
            /// <summary>
            /// Query placeholder
            /// </summary>
            /// <returns>Empty</returns>
            public string Empty() => string.Empty;
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
                Enum.TryParse<Account.Type>(type, out var accountType);
                Resolve(new CreateAccount(name, accountType));
                return true;
            }
        }
    }
}