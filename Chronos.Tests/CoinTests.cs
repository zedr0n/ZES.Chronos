using System;
using System.Threading;
using Chronos.Coins;
using Chronos.Coins.Commands;
using Chronos.Coins.Events;
using Chronos.Coins.Queries;
using Chronos.Core;
using NodaTime.Extensions;
using Xunit;
using Xunit.Abstractions;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Pipes;
using ZES.Tests;
using ZES.Utils;

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
            var timeline = container.GetInstance<ITimeline>();
           
            await await bus.CommandAsync(new CreateCoin("Bitcoin", "BTC"));
            var now = timeline.Now;
            Thread.Sleep(50);
            
            await await bus.CommandAsync(new CreateCoin("Ethereum", "ETH"));

            await bus.Equal(new StatsQuery(), s => s.NumberOfCoins, 2);
            
            var historicalQuery = new HistoricalQuery<StatsQuery, Stats>(new StatsQuery(), now);
            await bus.Equal(historicalQuery, s => s.NumberOfCoins, 1);
            
            var liveQuery = new HistoricalQuery<StatsQuery, Stats>(new StatsQuery(), DateTime.UtcNow.ToInstant());
            await bus.Equal(liveQuery, s => s.NumberOfCoins, 2);
        }

        [Fact]
        public async void CanGetWalletInfo()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();

            var btc = new Asset("Bitcoin", "BTC", Asset.Type.Coin);
            await await bus.CommandAsync(new CreateCoin("Bitcoin", "BTC"));
            await await bus.CommandAsync(new CreateWallet("0x1", "Bitcoin"));

            await await bus.CommandAsync(new ChangeWalletBalance("0x1", new Quantity(0.1, btc), null));

            await bus.Equal(new WalletInfoQuery("0x1"), s => s.Balance, 0.1);
            await bus.Equal(new WalletInfoQuery("0x1"), s => s.Asset, new Asset("Bitcoin", "BTC", Asset.Type.Coin));
        }

        [Fact]
        public async void CanTransferCoins()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            
            var btc = new Asset("Bitcoin", "BTC", Asset.Type.Coin);
            await await bus.CommandAsync(new CreateCoin("Bitcoin", "BTC"));
            await await bus.CommandAsync(new CreateWallet("0x1", "Bitcoin"));
            await await bus.CommandAsync(new CreateWallet("0x2", "Bitcoin"));
            
            await await bus.CommandAsync(new ChangeWalletBalance("0x1", new Quantity(0.1, btc), null));

            await await bus.CommandAsync(new TransferCoins("0x0", "0x1", "0x2", new Quantity(0.05, btc), new Quantity(0.001, btc)));
            await bus.Equal(new WalletInfoQuery("0x1"), s => s.Balance, 0.049);
            await bus.Equal(new WalletInfoQuery("0x2"), s => s.Balance, 0.05);
        }
    }
}