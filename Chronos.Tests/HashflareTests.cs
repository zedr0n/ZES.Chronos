using System;
using System.Reactive.Linq;
using Chronos.Hashflare;
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
    public class HashflareTests : ChronosTest
    {
        public HashflareTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [Fact]
        public async void CanRegisterHashflare()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var repository = container.GetInstance<IEsRepository<IAggregate>>();
            
            var time = ((DateTimeOffset)new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeMilliseconds(); 

            await await bus.CommandAsync(new RegisterHashflare("zedr0nre@gmail.com", time));

            var hashflare = await repository.Find<Hashflare.Hashflare>("Hashflare");
            Assert.NotNull(hashflare);
            Assert.Equal("zedr0nre@gmail.com", hashflare.Username);

            var graph = container.GetInstance<IGraph>();
            await graph.Serialise(nameof(CanRegisterHashflare));
        }

        [Fact]
        public async void CanCreatePurchase()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            
            var time = ((DateTimeOffset)new DateTime(1970, 1, 1, 0, 0, 10, DateTimeKind.Utc)).ToUnixTimeMilliseconds(); 

            await await bus.CommandAsync(new RegisterHashflare("zedr0nre@gmail.com", time));
            await await bus.CommandAsync(new CreateContract("0", "SHA-256", 100, 1000, time));

            await bus.Equal(new StatsQuery(), s => s.BitcoinHashRate, 100);
            
            var historicalQuery = new HistoricalQuery<StatsQuery, HashflareStats>(new StatsQuery(), time + 100);
            var result = await bus.QueryAsync(historicalQuery);
            Assert.Equal(100, result.BitcoinHashRate);
        }

        [Fact]
        public async void CanAddMinedToContract()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            await await bus.CommandAsync(new RegisterHashflare("zedr0nre@gmail.com"));
 
            await await bus.CommandAsync(new CreateContract("0", "SHA-256", 100, 1000));
            await await bus.CommandAsync(new CreateContract("1", "SHA-256", 100, 1000));

            await await bus.CommandAsync(new AddMinedToHashflare("SHA-256", 0.1));

            await bus.Equal(new ContractStatsQuery("0"), c => c?.Mined, 0.05);
            await bus.Equal(new ContractStatsQuery("1"), c => c?.Mined, 0.05);
        }

        [Fact]
        public async void CanRetroactivelyAddMinedToContract()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            var graph = container.GetInstance<IGraph>();

            var time = timeline.Now;
            var lastTime = time + (60 * 1000);
            var midTime = (time + lastTime) / 2;
            
            await await bus.CommandAsync(new RegisterHashflare("zedr0nre@gmail.com"));
 
            await await bus.CommandAsync(new CreateContract("0", "SHA-256", 100, 1000));
            await await bus.CommandAsync(new RetroactiveCommand<CreateContract>(new CreateContract("1", "SHA-256", 100, 1000), lastTime));

            await await bus.CommandAsync(
                new RetroactiveCommand<AddMinedToHashflare>(new AddMinedToHashflare("SHA-256", 0.01), midTime));

            await bus.Equal(new ContractStatsQuery("0"), c => c.Mined, 0.01);

            await await bus.CommandAsync(
                new RetroactiveCommand<AddMinedToHashflare>(new AddMinedToHashflare("SHA-256", 0.01), lastTime + 500));
            
            await bus.Equal(new HistoricalQuery<ContractStatsQuery, ContractStats>(new ContractStatsQuery("0"), lastTime + 1000), c => c.Mined, 0.01 * 1.5);
            await bus.Equal(new HistoricalQuery<ContractStatsQuery, ContractStats>(new ContractStatsQuery("1"), lastTime + 1000), c => c.Mined, 0.01 * 0.5);

            await graph.Serialise(nameof(CanRetroactivelyAddMinedToContract));
        }

        [Fact]
        public async void CanHandleInvalidRetroactiveSaga()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            
            var time = timeline.Now;
            var lastTime = time + (60 * 1000);
            var midTime = (time + lastTime) / 2;
            var lastQuarterTime = (midTime + lastTime) / 2;
            
            await await bus.CommandAsync(new RegisterHashflare("zedr0nre@gmail.com"));
 
            await await bus.CommandAsync(new CreateContract("0", "SHA-256", 100, 1000));
            await await bus.CommandAsync(
                new RetroactiveCommand<AddMinedToHashflare>(new AddMinedToHashflare("SHA-256", 0.01), lastTime));
            
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