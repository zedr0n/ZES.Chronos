using System.Reflection;
using SimpleInjector;
using ZES.Infrastructure.Attributes;
using ZES.Utils;

namespace Chronos.Accounts
{
    public static class Config
    {
        [Registration]
        public static void RegisterAll(Container c)
        {
            c.RegisterAll(Assembly.GetExecutingAssembly());
        }
        
        [RootQuery]
        public class Query
        {
        }

        [RootMutation]
        public class Mutation
        {
        }
    }
}