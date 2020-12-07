using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Queries;
using NodaTime;
using NodaTime.Extensions;
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
            
            var time = new DateTime(1970, 1, 1, 12, 0, 10, DateTimeKind.Utc).ToInstant(); 

            await await bus.CommandAsync(new RetroactiveCommand<RegisterHashflare>(new RegisterHashflare("user@mail.com"), time));

            var hashflare = await repository.Find<Hashflare.Hashflare>("Hashflare");
            Assert.NotNull(hashflare);
            Assert.Equal("user@mail.com", hashflare.Username);

            var graph = container.GetInstance<IGraph>();
            await graph.Serialise(nameof(CanRegisterHashflare));
        }

        [Fact]
        public async void CanCreatePurchase()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            
            var time = new DateTime(1970, 1, 1, 12, 0, 10, DateTimeKind.Utc).ToInstant(); 

            await await bus.CommandAsync(new RetroactiveCommand<RegisterHashflare>(new RegisterHashflare("user@mail.com"), time));
            await await bus.CommandAsync(new RetroactiveCommand<CreateContract>(new CreateContract("0", "SHA-256", 100, 1000), time));

            await bus.Equal(new HashflareStatsQuery(), s => s.BitcoinHashRate, 100);
            
            var historicalQuery = new HistoricalQuery<HashflareStatsQuery, HashflareStats>(new HashflareStatsQuery(), time + Duration.FromMilliseconds(100));
            var result = await bus.QueryAsync(historicalQuery);
            Assert.Equal(100, result.BitcoinHashRate);
        }

        [Fact]
        public async void CanAddMinedToContract()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            await await bus.CommandAsync(new RegisterHashflare("user@mail.com"));
 
            await await bus.CommandAsync(new CreateContract("0", "SHA-256", 100, 1000));
            await await bus.CommandAsync(new CreateContract("1", "SHA-256", 100, 1000));

            await await bus.CommandAsync(new AddMinedCoinToHashflare("SHA-256", 0.1));

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
            var lastTime = time + Duration.FromSeconds(1); 
            var midTime = time + ((lastTime - time) / 2);
            
            await await bus.CommandAsync(new RegisterHashflare("user@mail.com"));
 
            await await bus.CommandAsync(new CreateContract("0", "SHA-256", 100, 1000));
            await await bus.CommandAsync(new RetroactiveCommand<CreateContract>(new CreateContract("1", "SHA-256", 100, 1000), lastTime));

            await await bus.CommandAsync(
                new RetroactiveCommand<AddMinedCoinToHashflare>(new AddMinedCoinToHashflare("SHA-256", 0.01), midTime));

            await bus.Equal(new ContractStatsQuery("0"), c => c.Mined, 0.01);

            await await bus.CommandAsync(
                new RetroactiveCommand<AddMinedCoinToHashflare>(new AddMinedCoinToHashflare("SHA-256", 0.01), lastTime + Duration.FromMilliseconds(500)));
            
            await bus.Equal(new HistoricalQuery<ContractStatsQuery, ContractStats>(new ContractStatsQuery("0"), lastTime + Duration.FromSeconds(1)), c => c.Mined, 0.01 * 1.5);
            await bus.Equal(new HistoricalQuery<ContractStatsQuery, ContractStats>(new ContractStatsQuery("1"), lastTime + Duration.FromSeconds(1)), c => c.Mined, 0.01 * 0.5);

            await graph.Serialise(nameof(CanRetroactivelyAddMinedToContract));
        }
        
        [Fact]
        public async void CanRetroactivelyAddPurchase()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            
            var time = timeline.Now;
            var lastTime = time + Duration.FromSeconds(30);
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
        
        [Fact]
        public async void CanRetroactivelyAddPurchaseWithSeveralMined()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            
            var time = timeline.Now;
            var lastTime = time + Duration.FromSeconds(1);
            var ultimateTime = lastTime + Duration.FromSeconds(1);
            var midTime = time + ((lastTime - time) / 2);
            
            await await bus.CommandAsync(new RegisterHashflare("user@mail.com"));
 
            await await bus.CommandAsync(new CreateContract("0", "SHA-256", 100, 1000));
            await await bus.CommandAsync(
                new RetroactiveCommand<AddMinedCoinToHashflare>(new AddMinedCoinToHashflare("SHA-256", 0.01), lastTime));
            await await bus.CommandAsync(
                new RetroactiveCommand<AddMinedCoinToHashflare>(new AddMinedCoinToHashflare("SHA-256", 0.1), ultimateTime));
            
            await await bus.CommandAsync(new RetroactiveCommand<CreateContract>(new CreateContract("1", "SHA-256", 100, 1000), midTime));
            
            await bus.Equal(new ContractStatsQuery("0"), c => c.Mined, 0.055);
            await bus.Equal(new ContractStatsQuery("1"), c => c.Mined, 0.055);
        }
    }
}