using System.Linq;
using System.Reactive.Linq;
using Chronos.Accounts;
using Chronos.Accounts.Commands;
using Chronos.Accounts.Queries;
using Chronos.Coins.Commands;
using Chronos.Coins.Queries;
using Chronos.Core;
using Chronos.Core.Commands;
using Chronos.Core.Queries;
using Chronos.Hashflare.Commands;
using Xunit;
using Xunit.Abstractions;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;
using ZES.Interfaces;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Pipes;
using ZES.Tests;
using ZES.Utils;
using StatsQuery = Chronos.Accounts.Queries.StatsQuery;

namespace Chronos.Tests
{
    public class AccountTests : ChronosTest
    {
        public AccountTests(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
        }

        [Fact]
        public async void CanCreateAccount()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var repository = container.GetInstance<IEsRepository<IAggregate>>();

            await await bus.CommandAsync(new CreateAccount("Account", AccountType.Saving));

            var account = await repository.Find<Account>("Account");
            Assert.NotNull(account); 
            Assert.Equal("Account", account.Id);

            await bus.Equal(new StatsQuery(), s => s.NumberOfAccounts, 1);
        }

        [Fact]
        public async void CanCreateTransfer()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var manager = container.GetInstance<IBranchManager>();

            var usd = new Currency("USD");
            await bus.Command(new CreateAccount("Account", AccountType.Saving));
            await bus.Command(new CreateAccount("OtherAccount", AccountType.Saving));

            await bus.Command(new StartTransfer("Transfer", "Account", "OtherAccount", new Quantity(100, usd)));
            await manager.Ready;
            
            await bus.Equal(new AccountStatsQuery("Account", usd), a => a.Balance, new Quantity(-100, usd));
            await bus.Equal(new AccountStatsQuery("OtherAccount", usd), a => a.Balance, new Quantity(100, usd));

            await bus.IsTrue(new TransactionListQuery("Account"), l => l.TxId.Contains("Transfer[From]"));
            await bus.IsTrue(new TransactionListQuery("OtherAccount"), l => l.TxId.Contains("Transfer[To]"));
        }

        [Fact]
        public async void CanTrackWallet()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();

            await bus.Command(new CreateCoin("Bitcoin", "BTC"));
            var btc = (await bus.QueryAsync(new CoinInfoQuery("Bitcoin"))).Asset;
            
            await bus.Command(new CreateWallet("0x0", "Bitcoin"));
            await bus.Command(new MineCoin("0x0", new Quantity(0.1, btc), "Block"));

            await bus.Equal(new AccountStatsQuery("0x0", btc), a => a.Balance, new Quantity(0.1, btc));
            var txList = await bus.QueryAsync(new TransactionListQuery("0x0"));
            var mineTx = txList.TxId.SingleOrDefault();
            Assert.Equal("0x0[Block]", mineTx);
        }

        [Fact]
        public async void CanDepositAsset()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var repository = container.GetInstance<IEsRepository<IAggregate>>();
            var timeline = container.GetInstance<ITimeline>();
           
            var ccy = new Currency("USD");
            var asset = new Asset("Bitcoin", "BTC", Asset.Type.Coin);
            await await bus.CommandAsync(new RegisterAssetPair(AssetPair.Fordom(asset, ccy), asset, ccy));
            await await bus.CommandAsync(new AddQuote(AssetPair.Fordom(asset, ccy), timeline.Now.ToInstant(), 23000));
            
            await await bus.CommandAsync(new CreateAccount("Account", AccountType.Trading));
            await await bus.CommandAsync(new DepositAsset("Account", new Quantity(1.0, asset)));
            
            var account = await repository.Find<Account>("Account");
            Assert.Equal("Bitcoin", account.Assets[0]);

            await bus.Equal(new AccountStatsQuery("Account", asset), s => s.Balance, new Quantity(1.0, asset));
            await bus.Equal(new AccountStatsQuery("Account", ccy), s => s.Balance, new Quantity(23000, ccy));
        }
        
        [Fact]
        public async void CanAddTransaction()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
           
            await await bus.CommandAsync(new CreateAccount("Account", AccountType.Saving));

            var gbp = new Currency("GBP");
            var usd = new Currency("USD");

            await bus.Command(new RegisterAssetPair("GBPUSD", gbp, usd));
            await bus.Command(new AddQuote("GBPUSD", timeline.Now.ToInstant(), 1.2));

            await bus.Command(new RecordTransaction("Tx", new Quantity(100, gbp), Transaction.TransactionType.Spend, string.Empty));

            await bus.Command(new AddTransaction("Account", "Tx"));
            await bus.Equal(new AccountStatsQuery("Account", usd), a => a.Balance, new Quantity(-100 * 1.2, usd));
        }

        [Fact]
        public async void CanListTransactions()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
           
            await await bus.CommandAsync(new CreateAccount("Account", AccountType.Saving));

            var gbp = new Currency("GBP");
           
            await bus.Command(new RecordTransaction("Tx", new Quantity(100, gbp), Transaction.TransactionType.Income, string.Empty));
            await bus.Command(new RecordTransaction("Tx2", new Quantity(100, gbp), Transaction.TransactionType.Spend, string.Empty));

            await bus.Command(new AddTransaction("Account", "Tx"));
            await bus.Command(new AddTransaction("Account", "Tx2"));

            var list = await bus.QueryUntil(new TransactionListQuery("Account"), r => r.TxId.Length > 0);
            Assert.Contains("Tx", list.TxId);
            Assert.Contains("Tx2", list.TxId);
            
            await bus.Equal(new AccountStatsQuery("Account", gbp), a => a.Balance, new Quantity(0, gbp));
        }

        [Fact]
        public async void CanGetBalanceInUsd()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();

            var gbp = new Currency("GBP");
            var usd = new Currency("USD");

            await await bus.CommandAsync(new RegisterAssetPair(AssetPair.Fordom(gbp, usd), gbp, usd));

            var account = "Bank";
            await await bus.CommandAsync(new CreateAccount(account, AccountType.Saving));

            var txId = "ATM";
            await await bus.CommandAsync(new RecordTransaction(txId, new Quantity(100, gbp), Transaction.TransactionType.Income, null));

            await await bus.CommandAsync(new AddTransaction(account, txId));
            
            var assetsList = await bus.QueryAsync(new AssetPairsInfoQuery());
            var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == usd.AssetId);

            var txIds = await bus.QueryAsync(new TransactionListQuery(account));
            var txList = txIds.TxId.ToList().Select(tx => bus.QueryAsync(new TransactionInfoQuery(tx)).Result).ToList();

            foreach (var t in txList)
            {
                var fordom = AssetPair.Fordom(t.Quantity.Denominator, asset);
                var assetPairInfo = await bus.QueryAsync(new AssetPairInfoQuery(fordom));
                if (!assetPairInfo.QuoteDates.Any(d =>
                        d.InUtc().Year == t.Date.InUtc().Year && d.InUtc().Month == t.Date.InUtc().Month &&
                        d.InUtc().Day == t.Date.InUtc().Day))
                    // await await bus.CommandAsync(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(fordom), t.Date.InUtc().LocalDateTime.Date.AtMidnight().InUtc().ToInstant().ToTime()));
                    await await bus.CommandAsync(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(fordom), t.Date.ToTime()));
            }

            var balance = await bus.QueryAsync(new AccountStatsQuery(account, usd));
            Assert.NotNull(balance);
            Assert.NotNull(balance.Balance.Denominator);
        }

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
            await await bus.CommandAsync(new AddQuote(AssetPair.Fordom(asset, usd), timeline.Now.ToInstant(), 23000));

            await await bus.CommandAsync(new RegisterAssetPair(AssetPair.Fordom(gbp, usd), gbp, usd));
            await await bus.CommandAsync(new AddQuote(AssetPair.Fordom(gbp, usd), timeline.Now.ToInstant(), 1.3));
            
            await await bus.CommandAsync(new CreateAccount("Account", AccountType.Trading));
            await await bus.CommandAsync(new DepositAsset("Account", new Quantity(1.0, asset)));
            
            var account = await repository.Find<Account>("Account");
            Assert.Equal("Bitcoin", account.Assets[0]);

            await bus.Equal(new AccountStatsQuery("Account", gbp), s => s.Balance, new Quantity(23000 / 1.3, gbp));
        }

        [Fact]
        public async void CanProcessHashflare()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            var manager = container.GetInstance<IBranchManager>();

            var asset = new Asset("Bitcoin", "BTC", Asset.Type.Coin);
            var usd = new Currency("USD");
            await bus.Command(new RegisterAssetPair(AssetPair.Fordom(asset, usd), asset, usd));
            await bus.Command(new AddQuote(AssetPair.Fordom(asset, usd), timeline.Now.ToInstant(), 23000));
            
            await bus.Command(new RegisterHashflare("user@mail.com"));
            await manager.Ready;
            await bus.Command(new CreateContract("0", asset.Ticker, 100, 1000));
            await bus.Command(new AddMinedCoinToHashflare(asset.Ticker, 0.1));

            await bus.Equal(new AccountStatsQuery("Hashflare", usd), a => a.Balance, new Quantity(23000 * 0.1, usd));
        }
    }
}