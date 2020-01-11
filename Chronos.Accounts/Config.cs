using System.Reflection;
using SimpleInjector;
using ZES.Infrastructure.Attributes;
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
        public class Query
        {
        }

        /// <summary>
        /// Root graphql mutation for Accounts domain
        /// </summary>
        public class Mutation
        {
        }
    }
}