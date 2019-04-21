using Common.Logging;
using HotChocolate;
using HotChocolate.Execution;
using Xunit;
using Xunit.Abstractions;
using ZES.GraphQL;

using static Chronos.Coins.Schema;

namespace Chronos.Tests
{
    public class SchemaTests : ChronosTest
    {
        public SchemaTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async void CanCreateCoinWithSchema()
        {
            var container = CreateContainer();
            var log = container.GetInstance<ZES.Interfaces.ILog>(); 
            
            var schemaProvider = container.GetInstance<ISchemaProvider>();
            
            var schema = schemaProvider.Generate(typeof(Query), typeof(Mutation));            
            var executor = schema.MakeExecutable();
            
            await executor.ExecuteAsync(@"mutation { createCoin( command : { name : ""Bitcoin"", ticker : ""BTC"" } ) }");
            
            var statsResult = await executor.ExecuteAsync(@"{ stats( query : {  } ) { numberOfCoins } }") as IReadOnlyQueryResult;
            dynamic statsDict = statsResult?.Data["stats"];
            log.Info(statsDict);
            Assert.NotNull(statsDict);
            Assert.Equal(1, statsDict["numberOfCoins"]); 
        }
    }
}