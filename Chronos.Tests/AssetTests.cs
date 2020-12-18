using Chronos.Core;
using Chronos.Core.Commands;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using ZES.Interfaces;
using ZES.Interfaces.Pipes;

namespace Chronos.Tests
{
    public class AssetTests : ChronosTest
    {
        public AssetTests(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
        }

        [Fact]
        public async void CanUseCurrencyPair()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();

            var forAsset = new Currency("GBP");
            var domAsset = new Currency("USD");

            await bus.Command(new RegisterAssetPair("GBPUSD", forAsset, domAsset));
            await bus.Command(new AddQuote("GBPUSD", timeline.Now, 1.2));
        }
    }
}