using System.Reflection;
using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Queries;
using SimpleInjector;
using ZES.Infrastructure.Attributes;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.GraphQl;
using ZES.Interfaces;
using ZES.Interfaces.Pipes;
using ZES.Utils;

#pragma warning disable 1591

namespace Chronos.Hashflare
{
    public class Config
    {
        [Registration]
        public static void RegisterAll(Container c)
        {
            c.RegisterAll(Assembly.GetExecutingAssembly());
        }

        /// <inheritdoc />
        public class Queries : GraphQlQuery
        {
            /// <inheritdoc />
            public Queries(IBus bus) 
                : base(bus)
            {
            }

            public HashflareStats HashflareStats() 
                => Resolve(new HashflareStatsQuery());

            public HashflareStats HashflareStatsAsOf(long timestamp) 
                => Resolve(new HistoricalQuery<HashflareStatsQuery, HashflareStats>(new HashflareStatsQuery(), timestamp));

            public ContractStats ContractStats(string txId) 
                => Resolve(new ContractStatsQuery(txId));

            public ContractStats ContractStatsAsOf(string txId, long timestamp) 
                => Resolve(new HistoricalQuery<ContractStatsQuery, ContractStats>(new ContractStatsQuery(txId), timestamp));
        }

        public class Mutations : GraphQlMutation
        {
            public Mutations(IBus bus, ILog log)
                : base(bus, log)
            {
            }

            public bool RegisterHashflare(string username, long timestamp) 
                => Resolve(new RetroactiveCommand<RegisterHashflare>(new RegisterHashflare(username), timestamp));

            public bool BuyHashrate(string txId, string type, int quantity, int total, long timestamp) 
                => Resolve(new RetroactiveCommand<CreateContract>(new CreateContract(txId, type, quantity, total), timestamp));

            public bool AddMinedAmount(string type, double quantity, long timestamp) 
                => Resolve(new RetroactiveCommand<AddMinedCoinToHashflare>(new AddMinedCoinToHashflare(type, quantity), timestamp));
        }
    }
}