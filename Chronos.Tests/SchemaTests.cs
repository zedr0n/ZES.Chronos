using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using Xunit;
using Xunit.Abstractions;
using ZES.GraphQL;

namespace Chronos.Tests
{
    public class SchemaTests : ChronosTest
    {
        public SchemaTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [Fact]
        public async void CanCreateCoinWithSchema()
        {
            var container = CreateContainer();
            var log = container.GetInstance<ZES.Interfaces.ILog>(); 
            
            var schemaProvider = container.GetInstance<ISchemaProvider>();

            var executor = schemaProvider.Build();
            log.Info(executor.Schema);
            
            var commandResult = await executor.ExecuteAsync(@"mutation { createCoin( coin : ""Bitcoin"", ticker : ""BTC"" ) }");
            if (commandResult.Errors != null)
            {
                foreach (var e in commandResult.Errors)
                    log.Error(e.Message, this);
            }

            var statsResult = await executor.ExecuteAsync(@"{ stats { numberOfCoins } }") as IReadOnlyQueryResult;
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
    }
}