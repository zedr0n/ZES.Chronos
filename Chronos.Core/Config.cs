using System.Reflection;
using SimpleInjector;
using ZES.Infrastructure;
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
    }
}