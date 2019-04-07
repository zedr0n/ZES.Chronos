using System;
using System.Collections.Generic;
using Chronos.Coins;
using Chronos.Coins.Commands;
using Chronos.Coins.Queries;
using SimpleInjector;
using Xunit;
using Xunit.Abstractions;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Pipes;
using ZES.Tests;
using static ZES.ObservableExtensions;

namespace Chronos.Tests
{

    public class ChronosTest : Test
    {
        protected ChronosTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }
        
        protected override Container CreateContainer(List<Action<Container>> registrations = null)
        {
            var regs = new List<Action<Container>>
            {
                Config.RegisterAll
            };
            if(registrations != null)
                regs.AddRange(registrations);

            return base.CreateContainer(regs);
        }
    }
    
    public class CoinTests : ChronosTest
    {
        public CoinTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async void CanCreateCoin()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var repository = container.GetInstance<IDomainRepository>();
            
            var command = new CreateCoin("Bitcoin", "BTC");
            await bus.CommandAsync(command);

            var root = await RetryUntil(async () => await repository.Find<Coin>("Bitcoin"));
            Assert.Equal("Bitcoin",root.Id);
        }

        [Fact]
        public async void CanGetCoinInfo()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            
            var command = new CreateCoin("Bitcoin", "BTC");
            await bus.CommandAsync(command);

            var query = new CoinInfoQuery("Bitcoin");
            var coinInfo = await RetryUntil(async () => await bus.QueryAsync(query));
            
            Assert.Equal("BTC", coinInfo.Ticker);
        }
    }
}