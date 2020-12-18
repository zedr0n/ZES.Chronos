using Chronos.Accounts;
using Chronos.Accounts.Commands;
using Chronos.Accounts.Queries;
using Chronos.Core;
using Chronos.Core.Commands;
using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Queries;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;
using ZES.Interfaces.Domain;
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
        public async void CanGetAssetTriangulationPrice()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var repository = container.GetInstance<IEsRepository<IAggregate>>();
            var timeline = container.GetInstance<ITimeline>();
           
            var usd = new Currency("USD");
            var gbp = new Currency("GBP"); 
            var asset = new Asset("Bitcoin", "BTC", Asset.Type.Coin);
            await await bus.CommandAsync(new RegisterAssetPair(AssetPair.Fordom(asset, usd), asset, usd));
            await await bus.CommandAsync(new AddQuote(AssetPair.Fordom(asset, usd), timeline.Now, 23000));

            await await bus.CommandAsync(new RegisterAssetPair(AssetPair.Fordom(gbp, usd), gbp, usd));
            await await bus.CommandAsync(new AddQuote(AssetPair.Fordom(gbp, usd), timeline.Now, 1.3));
            
            await await bus.CommandAsync(new CreateAccount("Account", AccountType.Trading));
            await await bus.CommandAsync(new DepositAsset("Account", new Quantity(1.0, asset)));
            
            var account = await repository.Find<Account>("Account");
            Assert.Equal("Bitcoin", account.Assets[0]);

            await bus.Equal(new AccountStatsQuery("Account", gbp), s => s.Balance, new Quantity(23000 / 1.3, gbp));
        }
    }
}