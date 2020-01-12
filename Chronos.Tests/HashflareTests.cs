using System;
using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Queries;
using Xunit;
using Xunit.Abstractions;
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

            var graph = container.GetInstance<IQGraph>();
            graph.Serialise(nameof(CanRegisterHashflare));
        }

        [Fact]
        public async void CanCreatePurchase()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            
            var time = ((DateTimeOffset)new DateTime(1970, 1, 1, 0, 0, 10, DateTimeKind.Utc)).ToUnixTimeMilliseconds(); 

            await await bus.CommandAsync(new RegisterHashflare("zedr0nre@gmail.com", time));
            await await bus.CommandAsync(new CreatePurchase("0", "SHA-256", 100, 1000, time));

            await bus.Equal(new StatsQuery(), s => s.BitcoinHashRate, 0);
        }
    }
}