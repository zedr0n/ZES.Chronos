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

            await await bus.CommandAsync(new RegisterHashflare("zedr0nre@gmail.com"));

            var hashflare = await repository.Find<Hashflare.Hashflare>("Hashflare");
            Assert.NotNull(hashflare);
            Assert.Equal("zedr0nre@gmail.com", hashflare.Username);
        }

    }
}