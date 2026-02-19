using System;
using System.Threading;
using System.Threading.Tasks;
using Chronos.Accounts.Queries;
using Chronos.Coins;
using Chronos.Coins.Commands;
using Chronos.Coins.Queries;
using Chronos.Core;
using Chronos.Core.Queries;
using NodaTime.Extensions;
using Xunit;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Infrastructure;
using ZES.Interfaces.Net;
using ZES.TestBase;
using ZES.Utils;
using Stats = Chronos.Coins.Queries.Stats;
using StatsQuery = Chronos.Coins.Queries.StatsQuery;

namespace Chronos.Tests
{
    public class CoinTests : ChronosTest
    {
        public CoinTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [Fact]
        public async Task CanCreateCoin()
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
        public async Task CanGetCoinInfo()
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
        public async Task CanGetNumberOfCoins()
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
            
            var liveQuery = new HistoricalQuery<StatsQuery, Stats>(new StatsQuery(), DateTime.UtcNow.ToInstant().ToTime());
            await bus.Equal(liveQuery, s => s.NumberOfCoins, 2);
        }

        [Fact]
        public async Task CanGetWalletInfo()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();

            var btc = new Asset("Bitcoin", "BTC", AssetType.Coin);
            await await bus.CommandAsync(new CreateCoin("Bitcoin", "BTC"));
            await await bus.CommandAsync(new CreateWallet("0x1", "Bitcoin"));

            await await bus.CommandAsync(new ChangeWalletBalance("0x1", new Quantity(0.1, btc), null));

            await bus.Equal(new WalletInfoQuery("0x1"), s => s.Balance, 0.1);
            await bus.Equal(new WalletInfoQuery("0x1"), s => s.Asset, new Asset("Bitcoin", "BTC", AssetType.Coin));
        }

        [Fact]
        public async Task CanTransferCoins()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            
            var btc = new Asset("Bitcoin", "BTC", AssetType.Coin);
            await await bus.CommandAsync(new CreateCoin("Bitcoin", "BTC"));
            await await bus.CommandAsync(new CreateWallet("0x1", "Bitcoin"));
            await await bus.CommandAsync(new CreateWallet("0x2", "Bitcoin"));
            
            await await bus.CommandAsync(new ChangeWalletBalance("0x1", new Quantity(0.1, btc), null));

            await await bus.CommandAsync(new TransferCoins("0x0", "0x1", "0x2", new Quantity(0.05, btc), new Quantity(0.001, btc)));
            await bus.Equal(new WalletInfoQuery("0x1"), s => s.Balance, 0.049);
            await bus.Equal(new WalletInfoQuery("0x2"), s => s.Balance, 0.05);
        }

        [Fact]
        public async Task CanGetDailyOutflow()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();

            var address = "H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ";
            var server = Environment.GetEnvironmentVariable("REMOTESERVER");
            if (server == null)
                return;

            var url = $@"http://{server}/api/v1/address/{address}";
            await connector.SetAsync(
                url,
                @"{""hash"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""balance"":""186815.932500054"",""nonce"":134,""txs"":[{""hash"":""A5Mg5zDh3xVQhpAY8koohtCyPX6vghQ9c1pw9LJKRzRY"",""amount"":""133068.486417894"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""CpbsH6rZNXoFNBYfy35xbtuf9BY8di26VGpUVuTbzhny"",""estimated"":""133069.486417894"",""receiveTime"":1613670559954,""nonce"":134},{""hash"":""2ftLgeaSarrZfdabT9AQAvhzouMwuU4ewx4NGSfJKVHh"",""amount"":""45414.20099998"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""FPwfDhAdtcn2F2YrMCXkNupLXhJcPLHcVPx9NxkYW8Fo"",""estimated"":""45415.20099998"",""receiveTime"":1613069362300,""nonce"":133},{""hash"":""FnUKc2kiiGYJYfYkS3rvYuVNAPHqoevP6ixDGenMznD6"",""amount"":""49603.704999972"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""FKBTAjHuLr7ySUQusXcTtKJH1a7DbT7kr3MkJg9oZfcM"",""estimated"":""49604.704999972"",""receiveTime"":1612858421743,""nonce"":132},{""hash"":""j5hiV4tp3MuBxoJSDdPCjJXebGuz2dySTwQ9d6KuhBs"",""amount"":""61539.806999989"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""8RoLrAgnTGUupeBkUq8MuG6S7PLLgJpZDo6dNdxwnS6Q"",""estimated"":""61540.806999989"",""receiveTime"":1612638878446,""nonce"":131},{""hash"":""DmyxKt6ZwJUboMMFJpj3DCGFCS12QAbaLtwdV8Kwu7wu"",""amount"":""94078.610249994"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""DERvnzYFvsTGqEZg1R8jfN3mJkbkNTHdTseYQesnZA8"",""estimated"":""94079.610249994"",""receiveTime"":1612339520341,""nonce"":130},{""hash"":""6VXmCJ1qCGn5ffZnEPVW6ZsX3pMmFhbdVGwJ33L2NMpV"",""amount"":""224111.828250902"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""FMviisoS8UrEYZjC6eCdeTxCdN6rMeGGQXArAjyX7saz"",""estimated"":""224112.828250902"",""receiveTime"":1611909582311,""nonce"":129},{""hash"":""Ap38BdQrvtpYzasgLrTdNAfFw66y4QAtRyz8Qc8hTRVq"",""amount"":""220162.486625986"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""6HCKVWxHej3N5srrE7cizWsqBg9U9gnRFRp5kU8jpSun"",""estimated"":""220163.486625986"",""receiveTime"":1611057970253,""nonce"":128},{""hash"":""7AtnzmeYzzEQCPxuMeDG8Se1RJe9w9HS2ELR3tkw2tMh"",""amount"":""79089.058999969"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""2VB7w2XgaVDfKX558sDF3zM2JcjqEKDVYsQSf9yGNK1o"",""estimated"":""79090.058999969"",""receiveTime"":1610214893845,""nonce"":127},{""hash"":""Hi55GavivzPWLx7FbALNs1NBfejN8VFiJ7FkT5jYxS74"",""amount"":""120364.288999965"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""8Y6nW3v6sHDJPX63SqJksfU6XMPoTsFYjW8ekTLRdNdS"",""estimated"":""120365.288999965"",""receiveTime"":1609914014564,""nonce"":126},{""hash"":""J3GhbL75Kwja2ivEenJWuysP8uMnTo1Ubp7gJmffa5L6"",""amount"":""275166.383249919"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""9tHpDE2JR1VUcnXb9GfPi88MyX1D1nJDwiGSum9uPyUM"",""estimated"":""275167.383249919"",""receiveTime"":1609493537043,""nonce"":125}],""pendings"":[],""minedBlocks"":[{""blockhash"":""A2hGkq7QLwZbwVtMNnSXy8SNwZpo6tt3G7MnA5ue7fHm"",""timestamp"":1614527085989,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""4KcHAHynVrchWtAqhxoGD8c6UptobzGTmzquYZPQTTEL"",""timestamp"":1614527009705,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""3qqw3tKg6ktF58tQf5cikRRhEmAcRftgGcYUcdfZHVMt"",""timestamp"":1614526976156,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""X6dXxbrkngmH2q6jPizKdDUMzSw3TdDLLLfQCtk1ZSB"",""timestamp"":1614526908367,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""2WvBHphCcUbVpKLVsKKAVv6evfCDP8iWSXiMqGzxNnvt"",""timestamp"":1614526890291,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""EWgwb35C8NwvvM6fJo2gWwhPNK7zyZfXmAdSFEweAUuc"",""timestamp"":1614526813853,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""8YMZPbp87LfJhpfWXhPBr2Vc9zZCWRLHz5DtYH9UNWSS"",""timestamp"":1614526772180,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""4zd1E4VaAFF33w5TUjBjs72T3yJQct4Tvx4ANvoQEfSv"",""timestamp"":1614526731272,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""8a6enPgJbha6ntnAkLpGAFTi5cSPxPK9gd7CPK7JQaF5"",""timestamp"":1614526729408,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""4qhDmvqe79mJzU3CgqHHzvX4HDyvunhVBYkDTiroNbz2"",""timestamp"":1614526714026,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""}],""pendingAmount"":""0""}");

            url = $"http://{server}/api/v1/nextTxs/{address}/A5Mg5zDh3xVQhpAY8koohtCyPX6vghQ9c1pw9LJKRzRY/0";
            await connector.SetAsync(
                url,
                @"[{""hash"":""A5Mg5zDh3xVQhpAY8koohtCyPX6vghQ9c1pw9LJKRzRY"",""amount"":""133068.486417894"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""CpbsH6rZNXoFNBYfy35xbtuf9BY8di26VGpUVuTbzhny"",""estimated"":""133069.486417894"",""receiveTime"":1613670559954,""nonce"":134},{""hash"":""2ftLgeaSarrZfdabT9AQAvhzouMwuU4ewx4NGSfJKVHh"",""amount"":""45414.20099998"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""FPwfDhAdtcn2F2YrMCXkNupLXhJcPLHcVPx9NxkYW8Fo"",""estimated"":""45415.20099998"",""receiveTime"":1613069362300,""nonce"":133},{""hash"":""FnUKc2kiiGYJYfYkS3rvYuVNAPHqoevP6ixDGenMznD6"",""amount"":""49603.704999972"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""FKBTAjHuLr7ySUQusXcTtKJH1a7DbT7kr3MkJg9oZfcM"",""estimated"":""49604.704999972"",""receiveTime"":1612858421743,""nonce"":132},{""hash"":""j5hiV4tp3MuBxoJSDdPCjJXebGuz2dySTwQ9d6KuhBs"",""amount"":""61539.806999989"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""8RoLrAgnTGUupeBkUq8MuG6S7PLLgJpZDo6dNdxwnS6Q"",""estimated"":""61540.806999989"",""receiveTime"":1612638878446,""nonce"":131},{""hash"":""DmyxKt6ZwJUboMMFJpj3DCGFCS12QAbaLtwdV8Kwu7wu"",""amount"":""94078.610249994"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""DERvnzYFvsTGqEZg1R8jfN3mJkbkNTHdTseYQesnZA8"",""estimated"":""94079.610249994"",""receiveTime"":1612339520341,""nonce"":130},{""hash"":""6VXmCJ1qCGn5ffZnEPVW6ZsX3pMmFhbdVGwJ33L2NMpV"",""amount"":""224111.828250902"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""FMviisoS8UrEYZjC6eCdeTxCdN6rMeGGQXArAjyX7saz"",""estimated"":""224112.828250902"",""receiveTime"":1611909582311,""nonce"":129},{""hash"":""Ap38BdQrvtpYzasgLrTdNAfFw66y4QAtRyz8Qc8hTRVq"",""amount"":""220162.486625986"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""6HCKVWxHej3N5srrE7cizWsqBg9U9gnRFRp5kU8jpSun"",""estimated"":""220163.486625986"",""receiveTime"":1611057970253,""nonce"":128},{""hash"":""7AtnzmeYzzEQCPxuMeDG8Se1RJe9w9HS2ELR3tkw2tMh"",""amount"":""79089.058999969"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""2VB7w2XgaVDfKX558sDF3zM2JcjqEKDVYsQSf9yGNK1o"",""estimated"":""79090.058999969"",""receiveTime"":1610214893845,""nonce"":127},{""hash"":""Hi55GavivzPWLx7FbALNs1NBfejN8VFiJ7FkT5jYxS74"",""amount"":""120364.288999965"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""8Y6nW3v6sHDJPX63SqJksfU6XMPoTsFYjW8ekTLRdNdS"",""estimated"":""120365.288999965"",""receiveTime"":1609914014564,""nonce"":126},{""hash"":""J3GhbL75Kwja2ivEenJWuysP8uMnTo1Ubp7gJmffa5L6"",""amount"":""275166.383249919"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""9tHpDE2JR1VUcnXb9GfPi88MyX1D1nJDwiGSum9uPyUM"",""estimated"":""275167.383249919"",""receiveTime"":1609493537043,""nonce"":125}]");
             
            var manager = container.GetInstance<IBranchManager>();
            
            await bus.Command(new CreateCoin("Hycon", "HYC"));
            await bus.Command(new RetroactiveCommand<CreateWallet>(new CreateWallet(address, "Hycon"), "2018-06-01T00:00:00Z".ToInstant().Value.ToTime()));
            await manager.Ready;

            await bus.Command(new UpdateDailyOutflow(address, 0) { UseRemote = true });
            await bus.Equal(new WalletInfoQuery(address), i => i.Balance, -1302598.8557945699);
            var txInfo = await bus.QueryUntil(new TransactionListQuery(address), x => x.TxId.Length == 10);
            Assert.NotNull(txInfo.TxId);
            Assert.Equal(10, txInfo.TxId.Length);
        }

        [Fact]
        public async Task CanGetDailyMining()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var manager = container.GetInstance<IBranchManager>();
            var server = Environment.GetEnvironmentVariable("REMOTESERVER");
            if (server == null)
                return;
            
            var address = "H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ";
            var connector = container.GetInstance<IJSonConnector>();
            var url = $"http://{server}/api/v1/address/{address}";
            await connector.SetAsync(
                url,
                @"{""hash"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""balance"":""186815.932500054"",""nonce"":134,""txs"":[{""hash"":""A5Mg5zDh3xVQhpAY8koohtCyPX6vghQ9c1pw9LJKRzRY"",""amount"":""133068.486417894"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""CpbsH6rZNXoFNBYfy35xbtuf9BY8di26VGpUVuTbzhny"",""estimated"":""133069.486417894"",""receiveTime"":1613670559954,""nonce"":134},{""hash"":""2ftLgeaSarrZfdabT9AQAvhzouMwuU4ewx4NGSfJKVHh"",""amount"":""45414.20099998"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""FPwfDhAdtcn2F2YrMCXkNupLXhJcPLHcVPx9NxkYW8Fo"",""estimated"":""45415.20099998"",""receiveTime"":1613069362300,""nonce"":133},{""hash"":""FnUKc2kiiGYJYfYkS3rvYuVNAPHqoevP6ixDGenMznD6"",""amount"":""49603.704999972"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""FKBTAjHuLr7ySUQusXcTtKJH1a7DbT7kr3MkJg9oZfcM"",""estimated"":""49604.704999972"",""receiveTime"":1612858421743,""nonce"":132},{""hash"":""j5hiV4tp3MuBxoJSDdPCjJXebGuz2dySTwQ9d6KuhBs"",""amount"":""61539.806999989"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""8RoLrAgnTGUupeBkUq8MuG6S7PLLgJpZDo6dNdxwnS6Q"",""estimated"":""61540.806999989"",""receiveTime"":1612638878446,""nonce"":131},{""hash"":""DmyxKt6ZwJUboMMFJpj3DCGFCS12QAbaLtwdV8Kwu7wu"",""amount"":""94078.610249994"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""DERvnzYFvsTGqEZg1R8jfN3mJkbkNTHdTseYQesnZA8"",""estimated"":""94079.610249994"",""receiveTime"":1612339520341,""nonce"":130},{""hash"":""6VXmCJ1qCGn5ffZnEPVW6ZsX3pMmFhbdVGwJ33L2NMpV"",""amount"":""224111.828250902"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""FMviisoS8UrEYZjC6eCdeTxCdN6rMeGGQXArAjyX7saz"",""estimated"":""224112.828250902"",""receiveTime"":1611909582311,""nonce"":129},{""hash"":""Ap38BdQrvtpYzasgLrTdNAfFw66y4QAtRyz8Qc8hTRVq"",""amount"":""220162.486625986"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""6HCKVWxHej3N5srrE7cizWsqBg9U9gnRFRp5kU8jpSun"",""estimated"":""220163.486625986"",""receiveTime"":1611057970253,""nonce"":128},{""hash"":""7AtnzmeYzzEQCPxuMeDG8Se1RJe9w9HS2ELR3tkw2tMh"",""amount"":""79089.058999969"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""2VB7w2XgaVDfKX558sDF3zM2JcjqEKDVYsQSf9yGNK1o"",""estimated"":""79090.058999969"",""receiveTime"":1610214893845,""nonce"":127},{""hash"":""Hi55GavivzPWLx7FbALNs1NBfejN8VFiJ7FkT5jYxS74"",""amount"":""120364.288999965"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""8Y6nW3v6sHDJPX63SqJksfU6XMPoTsFYjW8ekTLRdNdS"",""estimated"":""120365.288999965"",""receiveTime"":1609914014564,""nonce"":126},{""hash"":""J3GhbL75Kwja2ivEenJWuysP8uMnTo1Ubp7gJmffa5L6"",""amount"":""275166.383249919"",""fee"":""1"",""from"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""to"":""HwHNxf2gXr7qkQERhUkbMQc568Tv4zT3"",""blockHash"":""9tHpDE2JR1VUcnXb9GfPi88MyX1D1nJDwiGSum9uPyUM"",""estimated"":""275167.383249919"",""receiveTime"":1609493537043,""nonce"":125}],""pendings"":[],""minedBlocks"":[{""blockhash"":""A2hGkq7QLwZbwVtMNnSXy8SNwZpo6tt3G7MnA5ue7fHm"",""timestamp"":1614527085989,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""4KcHAHynVrchWtAqhxoGD8c6UptobzGTmzquYZPQTTEL"",""timestamp"":1614527009705,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""3qqw3tKg6ktF58tQf5cikRRhEmAcRftgGcYUcdfZHVMt"",""timestamp"":1614526976156,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""X6dXxbrkngmH2q6jPizKdDUMzSw3TdDLLLfQCtk1ZSB"",""timestamp"":1614526908367,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""2WvBHphCcUbVpKLVsKKAVv6evfCDP8iWSXiMqGzxNnvt"",""timestamp"":1614526890291,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""EWgwb35C8NwvvM6fJo2gWwhPNK7zyZfXmAdSFEweAUuc"",""timestamp"":1614526813853,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""8YMZPbp87LfJhpfWXhPBr2Vc9zZCWRLHz5DtYH9UNWSS"",""timestamp"":1614526772180,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""4zd1E4VaAFF33w5TUjBjs72T3yJQct4Tvx4ANvoQEfSv"",""timestamp"":1614526731272,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""8a6enPgJbha6ntnAkLpGAFTi5cSPxPK9gd7CPK7JQaF5"",""timestamp"":1614526729408,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""4qhDmvqe79mJzU3CgqHHzvX4HDyvunhVBYkDTiroNbz2"",""timestamp"":1614526714026,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""}],""pendingAmount"":""0""}");

            url = $"http://{server}/api/v1/getMinedInfo/{address}/A2hGkq7QLwZbwVtMNnSXy8SNwZpo6tt3G7MnA5ue7fHm/0";
            await connector.SetAsync(
                url,
                @"[{""blockhash"":""A2hGkq7QLwZbwVtMNnSXy8SNwZpo6tt3G7MnA5ue7fHm"",""timestamp"":1614527085989,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""4KcHAHynVrchWtAqhxoGD8c6UptobzGTmzquYZPQTTEL"",""timestamp"":1614527009705,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""3qqw3tKg6ktF58tQf5cikRRhEmAcRftgGcYUcdfZHVMt"",""timestamp"":1614526976156,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""X6dXxbrkngmH2q6jPizKdDUMzSw3TdDLLLfQCtk1ZSB"",""timestamp"":1614526908367,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""2WvBHphCcUbVpKLVsKKAVv6evfCDP8iWSXiMqGzxNnvt"",""timestamp"":1614526890291,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""EWgwb35C8NwvvM6fJo2gWwhPNK7zyZfXmAdSFEweAUuc"",""timestamp"":1614526813853,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""8YMZPbp87LfJhpfWXhPBr2Vc9zZCWRLHz5DtYH9UNWSS"",""timestamp"":1614526772180,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""4zd1E4VaAFF33w5TUjBjs72T3yJQct4Tvx4ANvoQEfSv"",""timestamp"":1614526731272,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""8a6enPgJbha6ntnAkLpGAFTi5cSPxPK9gd7CPK7JQaF5"",""timestamp"":1614526729408,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""},{""blockhash"":""4qhDmvqe79mJzU3CgqHHzvX4HDyvunhVBYkDTiroNbz2"",""timestamp"":1614526714026,""miner"":""H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ"",""feeReward"":""12""}]");
 
            await bus.Command(new CreateCoin("Hycon", "HYC"));
            await bus.Command(new RetroactiveCommand<CreateWallet>(new CreateWallet(address, "Hycon"), "2018-06-01T00:00:00Z".ToInstant().Value.ToTime()));
            await manager.Ready;

            await bus.Command(new UpdateDailyMining(address, 0) { UseRemote = true });
            await manager.Ready;
            await bus.Equal(new WalletInfoQuery(address), i => i.MineQuantity, 120);
            var txList = await bus.QueryUntil(new TransactionListQuery(address));
            Assert.NotNull(txList.TxId);
            Assert.Equal("H2E7xSfMrPt2P96WWHQKR37Qpgfd6HskJ[Mining0_0]", txList.TxId[0]);
            await bus.Equal(new TransactionInfoQuery(txList.TxId[0]), t => t.Quantity.Amount, 120);
        }
    }
}