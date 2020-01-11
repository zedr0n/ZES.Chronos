using System.Reflection;
using SimpleInjector;
using ZES.Infrastructure.Attributes;
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
    }
}