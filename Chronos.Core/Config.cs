using System;
using System.Linq;
using System.Reflection;
using Chronos.Core.Commands;
using Chronos.Core.Queries;
using NodaTime;
using NodaTime.Text;
using SimpleInjector;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.GraphQl;
using ZES.Interfaces;
using ZES.Interfaces.Pipes;
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
        
        public class Query : GraphQlQuery
        {
            public Query(IBus bus)
                : base(bus)
            {
            }

            public TransactionInfo TransactionInfo(string txId) => Resolve(new TransactionInfoQuery(txId));
        }

        public class Mutation : GraphQlMutation
        {
            private readonly IBus _bus;
            
            public Mutation(IBus bus, ILog log)
                : base(bus, log)
            {
                _bus = bus;
            }

            public bool RecordTransaction(string txId, double amount, string assetId, string type, string comment, string date)
            {
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == assetId);
                var nDate = InstantPattern.General.Parse(date);
                if (asset == null || !Enum.TryParse<Transaction.TransactionType>(type, out var eType) || !nDate.Success)
                    return false;
                
                return Resolve(new RetroactiveCommand<RecordTransaction>(new RecordTransaction(txId, new Quantity(amount, asset), eType, comment), nDate.Value));
           }
        }
    }
}