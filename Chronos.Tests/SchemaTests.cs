using Chronos.Coins;
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
            
            var executor = schemaProvider.Generate(typeof(Config.Query), typeof(Config.Mutation));            
            
            await executor.ExecuteAsync(@"mutation { createCoin( command : { name : ""Bitcoin"", ticker : ""BTC"" } ) }");
            
            var statsResult = await executor.ExecuteAsync(@"{ stats( query : {  } ) { numberOfCoins } }") as IReadOnlyQueryResult;
            dynamic statsDict = statsResult?.Data["stats"];
            log.Info(statsDict);
            Assert.NotNull(statsDict);
            Assert.Equal(1, statsDict["numberOfCoins"]); 
        }
    }
}