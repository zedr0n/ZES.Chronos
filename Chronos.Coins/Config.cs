using System.Reflection;
using SimpleInjector;
using ZES;

namespace Chronos.Coins
{
    public static class Config
    {
        public static void RegisterAll(Container c)
        {
            c.RegisterAll(Assembly.GetExecutingAssembly());
        }
    }
}