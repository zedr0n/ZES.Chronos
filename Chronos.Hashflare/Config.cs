using System.Collections.Generic;
using System.Reflection;
using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Queries;
using NodaTime;
using SimpleInjector;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.GraphQl;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Infrastructure;
using ZES.Utils;

#pragma warning disable SA1600

namespace Chronos.Hashflare
{
    /// <summary>
    /// Hashflare config class
    /// </summary>
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
            /// <summary>
            /// Initializes a new instance of the <see cref="Queries"/> class.
            /// </summary>
            /// <param name="bus">Message bus</param>
            public Queries(IBus bus) 
                : base(bus)
            {
            }

            public HashflareStats HashflareStats() 
                => Resolve(new HashflareStatsQuery());

            public HashflareStats HashflareStatsAsOf(long timestamp) 
                => Resolve(new HistoricalQuery<HashflareStatsQuery, HashflareStats>(new HashflareStatsQuery(), Instant.FromUnixTimeMilliseconds(timestamp).ToTime()));

            public ContractStats ContractStats(string txId) 
                => Resolve(new ContractStatsQuery(txId));

            public ContractStats ContractStatsAsOf(string txId, long timestamp) 
                => Resolve(new HistoricalQuery<ContractStatsQuery, ContractStats>(new ContractStatsQuery(txId), Instant.FromUnixTimeMilliseconds(timestamp).ToTime()));
        }

        public class Mutations : GraphQlMutation
        {
            public Mutations(IBus bus, ILog log, IBranchManager manager)
                : base(bus, log, manager)
            {
            }

            public bool RegisterHashflare(string username, long timestamp, string guid) 
                => Resolve(new RetroactiveCommand<RegisterHashflare>(new RegisterHashflare(username), Instant.FromUnixTimeMilliseconds(timestamp).ToTime()) { Guid = guid } );

            public bool RegisterHashflareEx(RegisterHashflare command)  
                => Resolve(command);
            
            public bool BuyHashrate(string txId, string type, int quantity, int total, long timestamp, string guid) 
                => Resolve(new RetroactiveCommand<CreateContract>(new CreateContract(txId, type, quantity, total), Instant.FromUnixTimeMilliseconds(timestamp).ToTime()) { Guid = guid });

            public bool AddMinedAmount(string type, double quantity, long timestamp, string guid) 
                => Resolve(new RetroactiveCommand<AddMinedCoinToHashflare>(new AddMinedCoinToHashflare(type, quantity), Instant.FromUnixTimeMilliseconds(timestamp).ToTime()) { Guid = guid });

            public bool CreateContracts(string[] txId, string[] type, int[] quantity, int[] total, long[] timestamp, string[] guid)
            {
                var i = 0;
                var commands = new List<ICommand>();
                foreach (var t in txId)
                {
                    var command =
                        new RetroactiveCommand<CreateContract>(
                            new CreateContract(t, type[i], quantity[i], total[i]),
                            Instant.FromUnixTimeMilliseconds(timestamp[i]).ToTime()) { Guid = guid[i] };
                    commands.Add(command);
                    i++;
                }
                
                return Resolve(commands);
            }
            
            public bool AddMinedAmounts(string[] type, double[] quantity, long[] timestamp, string[] guid)
            {
                var i = 0;
                var commands = new List<ICommand>();
                foreach (var t in type)
                {
                    var command =
                        new RetroactiveCommand<AddMinedCoinToHashflare>(
                            new AddMinedCoinToHashflare(t, quantity[i]),
                            Instant.FromUnixTimeMilliseconds(timestamp[i]).ToTime()) { Guid = guid[i] };
                    commands.Add(command);
                    i++;
                }
                
                return Resolve(commands);
            }
        }
    }
}