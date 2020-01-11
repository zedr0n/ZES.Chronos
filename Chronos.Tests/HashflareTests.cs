using System;
using Chronos.Hashflare.Commands;
using Xunit;
using Xunit.Abstractions;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Pipes;

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
        }
    }
}