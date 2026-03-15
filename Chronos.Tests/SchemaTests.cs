using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chronos.Hashflare.Commands;
using HotChocolate.Execution;
using NodaTime;
using Xunit;
using ZES.GraphQL;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Infrastructure;

namespace Chronos.Tests
{
    public class SchemaTests : ChronosTest
    {
        public SchemaTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [Fact]
        public void CanCreateSchema()
        {
            var container = CreateContainer();
            var schemaProvider = container.GetInstance<ISchemaProvider>();
            var log = container.GetInstance<ILog>();
            
            var schema = schemaProvider.Build().Schema;
            log.Info(schema.ToString());
        }
        
        [Fact]
        public async Task CanCreateCoinWithSchema()
        {
            var container = CreateContainer();
            var log = container.GetInstance<ILog>(); 
            
            var schemaProvider = container.GetInstance<ISchemaProvider>();

            var executor = schemaProvider.Build();
            log.Info(executor.Schema);
            
            var commandResult = await executor.ExecuteAsync(@"mutation { createCoin( coin : ""Bitcoin"", ticker : ""BTC"" ) }", cancellationToken: TestContext.Current.CancellationToken);
            if (commandResult.Errors != null)
            {
                foreach (var e in commandResult.Errors)
                    log.Error(e.Message, this);
            }

            var statsResult = await executor.ExecuteAsync(@"{ stats { numberOfCoins } }", cancellationToken: TestContext.Current.CancellationToken) as IReadOnlyQueryResult;
            if (statsResult.Errors != null)
            {
                foreach (var e in statsResult.Errors)
                    log.Error(e.Message, this);
            }
            
            var statsDict = statsResult?.Data.SingleOrDefault().Value as IReadOnlyDictionary<string, object>;
            log.Info(statsDict);
            Assert.NotNull(statsDict);
            Assert.Equal(1, statsDict["numberOfCoins"]); 
        }
        
        [Fact]
        public async Task CanCreateContractsWithSchema()
        {
            var container = CreateContainer();
            var log = container.GetInstance<ILog>(); 
            
            var schemaProvider = container.GetInstance<ISchemaProvider>();
            var generator = container.GetInstance<IGraphQlGenerator>();
            var timeline = container.GetInstance<ITimeline>();
            
            var executor = schemaProvider.Build();
            
            var commandRegister = new RegisterHashflare() { Username = "blah@blah.com" };
            var mutationRegister = generator.Mutation(commandRegister);
            
            await executor.ExecuteAsync(mutationRegister, cancellationToken: TestContext.Current.CancellationToken);
            
            var time = timeline.Now;
            var timestamp = new[] { time, time - Duration.FromDays(1), time - Duration.FromDays(2) }.Select(t => t.ToInstant().ToUnixTimeMilliseconds()).ToArray();
            var mutation = $"mutation{{ createContracts(txId : [\"0\", \"1\", \"2\"], type: [\"SHA-256\", \"SHA-256\", \"SHA-256\"], quantity : [ 1, 1, 1 ], total : [100, 100, 100], timestamp: [ {timestamp[0]}, {timestamp[1]}, {timestamp[2]} ], guid : [ \"{Guid.NewGuid()}\", \"{Guid.NewGuid()}\", \"{Guid.NewGuid()}\" ] ) }}";
            
            await executor.ExecuteAsync(mutation, cancellationToken: TestContext.Current.CancellationToken);
            
            var statsResult = await executor.ExecuteAsync(@"query { hashflareStats { username bitcoinHashRate scryptHashRate } }", cancellationToken: TestContext.Current.CancellationToken) as IReadOnlyQueryResult;
            if (statsResult.Errors != null)
            {
                foreach (var e in statsResult.Errors)
                    log.Error(e.Message, this);
            }
            
            var statsDict = statsResult?.Data.SingleOrDefault().Value as IReadOnlyDictionary<string, object>;
            log.Info(statsDict);
            Assert.NotNull(statsDict);
            Assert.Equal(3, statsDict["bitcoinHashRate"]); 
        }
        
    }
}