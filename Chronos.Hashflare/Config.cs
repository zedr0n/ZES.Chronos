using System.Reflection;
using Chronos.Hashflare.Commands;
using SimpleInjector;
using ZES.Infrastructure.Attributes;
using ZES.Infrastructure.GraphQl;
using ZES.Interfaces;
using ZES.Interfaces.Pipes;
using ZES.Utils;

namespace Chronos.Hashflare
{
    public class Config
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

        public class Queries : GraphQlQuery
        {
            public Queries(IBus bus) 
                : base(bus)
            {
            }
        }

        public class Mutations : GraphQlMutation
        {
            public Mutations(IBus bus, ILog log)
                : base(bus, log)
            {
            }

            public bool RegisterHashflare(string username, long timestamp) => Resolve(new RegisterHashflare(username, timestamp));
        }
    }
}