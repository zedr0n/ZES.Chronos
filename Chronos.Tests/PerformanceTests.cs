using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Queries;
using Xunit;
using Xunit.Abstractions;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;
using ZES.Interfaces.Causality;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Pipes;
using ZES.Tests;

namespace Chronos.Tests
{
    public class PerformanceTests : ChronosTest
    {
        public PerformanceTests(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
        }

        protected override string LogEnabled => "INFO";
        
        [Fact]
        public async void CanRetroactivelyAddMinedToContractPerformance()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            var graph = container.GetInstance<IGraph>();

            var time = timeline.Now;
            var nAdds = 50;
            var totalBefore = 0.0;
            var lastTime = time + (60 * 1000 * ( (nAdds * 2) + 1 ));
            var midTime = (time + lastTime) / 2;
            
            await await bus.CommandAsync(new RegisterHashflare("user@mail.com"));

            var nContracts = 20;
            var addAfter = 0.01 / (nContracts + 1);
            var lastContractId = nContracts.ToString();

            var contractTimes = new SortedSet<long>();
            await await bus.CommandAsync(new CreateContract("0", "SHA-256", 100, 1000));
            contractTimes.Add(time);
            time = lastTime;
            do
            {
                await await bus.CommandAsync(new RetroactiveCommand<CreateContract>(
                    new CreateContract(nContracts.ToString(), "SHA-256", 100, 1000), time));
                
                contractTimes.Add(time);
                time -= 2 * 60 * 1000 * nAdds / nContracts;
            } 
            while (--nContracts > 0);
            
            while (nAdds-- > 0)
            {
                // await await bus.CommandAsync(new RetroactiveCommand<AddMinedCoinToHashflare>(new AddMinedCoinToHashflare("SHA-256", 0.01), midTime));
                await bus.CommandWithRetryAsync(
                    new RetroactiveCommand<AddMinedCoinToHashflare>(new AddMinedCoinToHashflare("SHA-256", 0.01), midTime));
                var totalHash = contractTimes.Where(t => t <= midTime).Sum(t => 100);
                totalBefore += 100.0 / totalHash * 0.01;
                midTime -= 60 * 1000;
            }

            await bus.Equal(new ContractStatsQuery("0"), c => Math.Round(c.Mined, 6), Math.Round(totalBefore, 6), TimeSpan.FromSeconds(10));

            await await bus.CommandAsync(
                new RetroactiveCommand<AddMinedCoinToHashflare>(new AddMinedCoinToHashflare("SHA-256", 0.01), lastTime + 500));

            await bus.Equal(new HistoricalQuery<ContractStatsQuery, ContractStats>(new ContractStatsQuery("0"), lastTime + 1000), c => Math.Round(c.Mined, 6), Math.Round(totalBefore + addAfter, 6), TimeSpan.FromSeconds(10));
            await bus.Equal(new HistoricalQuery<ContractStatsQuery, ContractStats>(new ContractStatsQuery(lastContractId), lastTime + 1000), c => Math.Round(c.Mined, 6), Math.Round(addAfter, 6), TimeSpan.FromSeconds(10));

            await graph.Serialise(nameof(CanRetroactivelyAddMinedToContractPerformance));
        }
    }
}