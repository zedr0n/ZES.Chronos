using System;
using Chronos.Coins;
using Chronos.Coins.Commands;
using Chronos.Coins.Queries;
using Xunit;
using Xunit.Abstractions;
using ZES.Infrastructure;
using ZES.Interfaces;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Pipes;
using static ZES.ObservableExtensions;

namespace Chronos.Tests
{
    public class CoinTests : ChronosTest
    {
        public CoinTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [Fact]
        public async void CanCreateCoin()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var repository = container.GetInstance<IEsRepository<IAggregate>>();
            
            var command = new CreateCoin("Bitcoin", "BTC");
            await await bus.CommandAsync(command);

            var root = await repository.Find<Coin>("Bitcoin");
            Assert.Equal("Bitcoin", root.Id);
        }

        [Fact]
        public async void CanGetCoinInfo()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            
            var command = new CreateCoin("Bitcoin", "BTC");
            await bus.CommandAsync(command);

            var query = new CoinInfoQuery("Bitcoin");
            var coinInfo = await bus.QueryUntil(query, c => c.Name == "Bitcoin"); 
            
            Assert.Equal("BTC", coinInfo.Ticker);
        }
        
        [Fact]
        public async void CanGetNumberOfCoins()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            
            await bus.CommandAsync(new CreateCoin("Bitcoin", "BTC"));
            var coinInfo = await bus.QueryUntil(new CoinInfoQuery("Bitcoin"));
            
            await bus.CommandAsync(new CreateCoin("Ethereum", "ETH"));
            
            var query = new StatsQuery(); 
            var stats = await bus.QueryUntil(query, s => s.NumberOfCoins > 0);

            Assert.Equal(2, stats.NumberOfCoins);
            
            var historicalQuery = new HistoricalQuery<StatsQuery, Stats>(query, coinInfo.CreatedAt);
            var historicalStats = await bus.QueryAsync(historicalQuery);
            
            Assert.Equal(1, historicalStats.NumberOfCoins);
            
            var liveQuery = new HistoricalQuery<StatsQuery, Stats>(query, DateTime.UtcNow.Ticks);
            var liveStats = await bus.QueryAsync(liveQuery);
            Assert.Equal(2, liveStats.NumberOfCoins);
        }
    }
}