using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Queries;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;
using ZES.Interfaces.Pipes;
using ZES.Tests;

namespace Chronos.Tests
{
    public class DebugTests : ChronosTest
    {
        public DebugTests(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
        }

        protected override string LogEnabled => "INFO";
        
        [Fact]
        public async void CanRetroactivelyAddPurchase()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            
            var time = timeline.Now;
            var lastTime = time + Duration.FromSeconds(1);
            var midTime = time + ((lastTime - time) / 2);
            var lastQuarterTime = midTime + ((lastTime - midTime) / 2);
            
            await await bus.CommandAsync(new RegisterHashflare("user@mail.com"));
 
            await await bus.CommandAsync(new CreateContract("0", "SHA-256", 100, 1000));
            await await bus.CommandAsync(
                new RetroactiveCommand<AddMinedCoinToHashflare>(new AddMinedCoinToHashflare("SHA-256", 0.01), lastTime));
            
            await await bus.CommandAsync(new RetroactiveCommand<CreateContract>(new CreateContract("1", "SHA-256", 100, 1000), midTime));
            
            await bus.Equal(new ContractStatsQuery("0"), c => c.Mined, 0.005);
            await bus.Equal(new ContractStatsQuery("1"), c => c.Mined, 0.005);
            
            await await bus.CommandAsync(new RetroactiveCommand<CreateContract>(new CreateContract("2", "SHA-256", 200, 1000), lastQuarterTime));
            await bus.Equal(new ContractStatsQuery("0"), c => c.Mined, 0.0025);
            await bus.Equal(new ContractStatsQuery("1"), c => c.Mined, 0.0025);
            await bus.Equal(new ContractStatsQuery("2"), c => c.Mined, 0.005);
        }
    }
}