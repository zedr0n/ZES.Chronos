using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Chronos.Accounts.Queries;
using Chronos.Core;
using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Queries;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;
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
        public async void CanReplayHashflareLog()
        {
            var path = "../../../Ad-hoc/ZES_Hashflare.json";
            if (!File.Exists(path))
                return;
            var result = await Replay(path);
            Assert.True(result.Result);
        }

        [Fact]
        public async void CanRetroactivelyAddMinedToContractPerformance()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            var graph = container.GetInstance<IGraph>();
            var log = container.GetInstance<ILog>();
            
            var stopWatch = Stopwatch.StartNew();

            var time = timeline.Now.ToInstant();
            var nAdds = 25;
            var total = (nAdds + 1) * 0.01;
            var totalBefore = 0.0;
            var lastTime = time + Duration.FromSeconds((nAdds * 2) + 1 );
            var midTime = time + ((lastTime - time) / 2);
            
            await await bus.CommandAsync(new RegisterHashflare("user@mail.com"));

            var nContracts = 10;
            var addAfter = 0.01 / (nContracts + 1);
            var lastContractId = nContracts.ToString();

            var contractTimes = new SortedSet<Instant>();
            await await bus.CommandAsync(new CreateContract("0", "SHA-256", 100, 1000));
            contractTimes.Add(time);
            time = lastTime;
            do
            {
                await await bus.CommandAsync(new RetroactiveCommand<CreateContract>(
                    new CreateContract(nContracts.ToString(), "SHA-256", 100, 1000), time.ToTime()));
                
                contractTimes.Add(time);
                time -= Duration.FromSeconds(2 * nAdds / nContracts);
            } 
            while (--nContracts > 0);
            
            while (nAdds-- > 0)
            {
                await bus.Command(
                    new RetroactiveCommand<AddMinedCoinToHashflare>(new AddMinedCoinToHashflare("SHA-256", 0.01), midTime.ToTime()),
                    1);
                var totalHash = contractTimes.Where(t => t <= midTime).Sum(t => 100);
                totalBefore += 100.0 / totalHash * 0.01;
                midTime -= Duration.FromSeconds(1);
            }

            lastTime += Duration.FromMilliseconds(500);
            await await bus.CommandAsync(
                new RetroactiveCommand<AddMinedCoinToHashflare>(new AddMinedCoinToHashflare("SHA-256", 0.01), lastTime.ToTime()));

            log.Info(log.StopWatch.Totals.ToImmutableSortedDictionary());
            log.Info($"Took {stopWatch.ElapsedMilliseconds}ms");
        }
    }
}