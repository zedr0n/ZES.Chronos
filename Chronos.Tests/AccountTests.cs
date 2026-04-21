using System.Linq;
using System.Threading.Tasks;
using Chronos.Accounts;
using Chronos.Accounts.Commands;
using Chronos.Accounts.Queries;
using Chronos.Coins.Commands;
using Chronos.Coins.Queries;
using Chronos.Core;
using Chronos.Core.Commands;
using Chronos.Core.Net;
using Chronos.Core.Queries;
using Chronos.Hashflare.Commands;
using NodaTime;
using Xunit;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Infrastructure;
using ZES.Interfaces.Net;
using ZES.TestBase;
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
        public async Task CanCreateAccount()
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
        public async Task CanCreateTransfer()
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
        public async Task CanTrackWallet()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();

            await connector.SetAsync(webSearchApi.GetUrl("Bitcoin-USD"), 
                "[{\"Code\":\"BTC-USD\",\"Exchange\":\"CC\",\"Name\":\"Bitcoin\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":68686.0390625,\"previousCloseDate\":\"2026-04-07\"}]");

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
        public async Task CanDepositAsset()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var repository = container.GetInstance<IEsRepository<IAggregate>>();
            var timeline = container.GetInstance<ITimeline>();
          
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();

            await connector.SetAsync(webSearchApi.GetUrl("Bitcoin-USD"), 
                "[{\"Code\":\"BTC-USD\",\"Exchange\":\"CC\",\"Name\":\"Bitcoin\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":68686.0390625,\"previousCloseDate\":\"2026-04-07\"}]");
            
            var ccy = new Currency("USD");
            var asset = new Asset("Bitcoin", AssetType.Coin);
            await await bus.CommandAsync(new RegisterAssetPair(AssetPair.Fordom(asset, ccy), asset, ccy));
            await await bus.CommandAsync(new AddQuote(AssetPair.Fordom(asset, ccy), timeline.Now.ToInstant(), 23000));
            
            await await bus.CommandAsync(new CreateAccount("Account", AccountType.Trading));
            await await bus.CommandAsync(new DepositAsset("Account", new Quantity(1.0, asset)));
            
            await bus.Equal(new AccountStatsQuery("Account", asset), s => s.Balance, new Quantity(1.0, asset));
            await bus.Equal(new AccountStatsQuery("Account", ccy), s => s.Balance, new Quantity(23000, ccy));
        }

        [Fact]
        public async Task CanPurchaseAssetWithQuote()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            
            var webSearchApi = webApiProvider.GetSearchApi();
            
            await bus.Command(new CreateAccount("Account", AccountType.Trading));
            
            var asset = new Asset("IUKD", AssetType.Equity);
            var ccy = new Currency("GBP");
            var quoteCcy = new Currency("GBX");
            const double price = 10.0;
            var count = 100.0;
            
            await connector.SetAsync(webSearchApi.GetUrl(asset.AssetId),
                "[{\"Code\":\"IUKD\",\"Exchange\":\"LSE\",\"Name\":\"iShares UK Dividend UCITS\",\"Type\":\"ETF\",\"Country\":\"UK\",\"Currency\":\"GBX\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":944,\"previousCloseDate\":\"2026-03-25\"},{\"Code\":\"IUKD\",\"Exchange\":\"SW\",\"Name\":\"iShares UK Dividend UCITS ETF GBP (Dist) CHF\",\"Type\":\"ETF\",\"Country\":\"Switzerland\",\"Currency\":\"CHF\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":9.948,\"previousCloseDate\":\"2026-03-25\"}]");
           
            await bus.Command(new RegisterAssetPair(AssetPair.Fordom(ccy, quoteCcy), ccy, quoteCcy));
            await bus.Command(new RegisterAssetPair(AssetPair.Fordom(asset, quoteCcy), asset, quoteCcy));
            await bus.Command(new AddQuote(AssetPair.Fordom(asset, quoteCcy), timeline.Now.ToInstant(), price*100));
            
            await bus.Command(new TransactAsset("Account",new Quantity(100, asset), new Quantity(count*price, ccy)));
            
            await bus.Equal(new AccountStatsQuery("Account", asset), s => s.Balance, new Quantity(0, asset));
            await bus.Equal(new AccountStatsQuery("Account", ccy), s => s.Balance, new Quantity(0, ccy));
        }
        
        [Fact]
        public async Task CanPurchaseAsset()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            
            var webSearchApi = webApiProvider.GetSearchApi();
            var webQuoteApi = webApiProvider.GetQuoteApi(AssetType.Equity, AssetType.Currency, true);
            
            await bus.Command(new CreateAccount("Account", AccountType.Trading));
            
            var asset = new Asset("IUKD", AssetType.Equity);
            var ccy = new Currency("GBP");
            var quoteCcy = new Currency("GBX");
            
            await connector.SetAsync(webSearchApi.GetUrl(asset.AssetId),
                "[{\"Code\":\"IUKD\",\"Exchange\":\"LSE\",\"Name\":\"iShares UK Dividend UCITS\",\"Type\":\"ETF\",\"Country\":\"UK\",\"Currency\":\"GBX\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":944,\"previousCloseDate\":\"2026-03-25\"},{\"Code\":\"IUKD\",\"Exchange\":\"SW\",\"Name\":\"iShares UK Dividend UCITS ETF GBP (Dist) CHF\",\"Type\":\"ETF\",\"Country\":\"Switzerland\",\"Currency\":\"CHF\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":9.948,\"previousCloseDate\":\"2026-03-25\"}]");
            await connector.SetAsync(webQuoteApi.GetUrl("IUKD.LSE", enforceCache: true),
                "{\"code\":\"IUKD.LSE\",\"timestamp\":1775748900,\"gmtoffset\":0,\"open\":995.9,\"high\":995.9,\"low\":984.3,\"close\":988.8,\"volume\":414031,\"previousClose\":988.2,\"change\":0.6,\"change_p\":0.0607}");
            
            await bus.Command(new RegisterAssetPair(AssetPair.Fordom(ccy, quoteCcy), ccy, quoteCcy));
            await bus.Command(new RegisterAssetPair(AssetPair.Fordom(asset, quoteCcy), asset, quoteCcy));
            
            await bus.Command(new TransactAsset("Account",new Quantity(100, asset), new Quantity(double.NaN, quoteCcy)));
           
            await bus.EqualDouble(new AccountStatsQuery("Account", asset), s => s.Balance.Amount, 0, precision: 6);
        
            var stats = await bus.QueryAsync(new AccountStatsQuery("Account", ccy));
            Assert.Single(stats.Positions);
            Assert.Equal(asset, stats.Positions[0].Denominator);
            Assert.Equal(988.8,stats.Values[0].Amount);
            Assert.Equal(-988.8, stats.CashBalance.Amount, 6);
            Assert.Equal(double.NaN, stats.CostBasis[0].Amount);
        }

        [Fact]
        public async Task CanComputeCostBasis()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            
            var webSearchApi = webApiProvider.GetSearchApi();
            
            await bus.Command(new RetroactiveCommand<CreateAccount>(new CreateAccount("Account", AccountType.Trading), Time.MinValue));
            
            var asset = new Asset("IUKD", AssetType.Equity);
            var ccy = new Currency("GBP");
            var quoteCcy = new Currency("GBX");
            
            await connector.SetAsync(webSearchApi.GetUrl(asset.AssetId),
                "[{\"Code\":\"IUKD\",\"Exchange\":\"LSE\",\"Name\":\"iShares UK Dividend UCITS\",\"Type\":\"ETF\",\"Country\":\"UK\",\"Currency\":\"GBX\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":944,\"previousCloseDate\":\"2026-03-25\"},{\"Code\":\"IUKD\",\"Exchange\":\"SW\",\"Name\":\"iShares UK Dividend UCITS ETF GBP (Dist) CHF\",\"Type\":\"ETF\",\"Country\":\"Switzerland\",\"Currency\":\"CHF\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":9.948,\"previousCloseDate\":\"2026-03-25\"}]");
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(ccy, quoteCcy), Time.MinValue));
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(asset, quoteCcy), Time.MinValue));
            
            var date = new LocalDateTime(2021, 8, 26, 12, 30).InUtc().ToInstant().ToTime();
            var date2 = new LocalDateTime(2021, 9, 29, 12, 30).InUtc().ToInstant().ToTime();
            var date3 = new LocalDateTime(2021, 10, 26, 12, 30).InUtc().ToInstant().ToTime();
            
            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("Account",new Quantity(100, asset), new Quantity(7.54*100, ccy)), date));
            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("Account",new Quantity(-50, asset), new Quantity(-7.19*50, ccy)), date2));
            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("Account",new Quantity(100, asset), new Quantity(7.4*100, ccy)), date3));
            
            var stats = await bus.QueryAsync(new AccountStatsQuery("Account", ccy));
            Assert.Equal(7.54*100 - 50*7.54 + 7.4*100, stats.CostBasis[0].Amount);
        }
        
        [Fact]
        public async Task CanAddTransaction()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();

            await connector.SetAsync(webSearchApi.GetUrl("GBPUSD"), 
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3234,\"previousCloseDate\":\"2026-04-06\"}]");

            await await bus.CommandAsync(new CreateAccount("Account", AccountType.Saving));

            var gbp = new Currency("GBP");
            var usd = new Currency("USD");

            await bus.Command(new RegisterAssetPair("GBPUSD", gbp, usd));
            await bus.Command(new AddQuote("GBPUSD", timeline.Now.ToInstant(), 1.2));

            await bus.Command(new CreateTransaction("Tx", new Quantity(-100, gbp), Transaction.TransactionType.General, string.Empty));

            await bus.Command(new AddTransaction("Account", "Tx"));
            await bus.Equal(new AccountStatsQuery("Account", usd), a => a.Balance, new Quantity(-100 * 1.2, usd));
        }

        [Fact]
        public async Task CanUpdateTransactionDetails()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();

            await await bus.CommandAsync(new CreateAccount("Account", AccountType.Saving));

            var gbp = new Currency("GBP");

            await bus.Command(new CreateTransaction("Tx", new Quantity(-100, gbp), Transaction.TransactionType.General, string.Empty));
            await bus.Command(new AddTransaction("Account", "Tx"));

            var res = await bus.QueryAsync(new TransactionInfoQuery("Tx"));

            await await bus.CommandAsync(new UpdateTransactionDetails("Tx", Transaction.TransactionType.Fee,
                res.Comment));
            
            res = await bus.QueryAsync(new TransactionInfoQuery("Tx"));
            Assert.Equal(Transaction.TransactionType.Fee, res.TransactionType);
        }
        
        [Fact]
        public async Task CanListTransactions()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
           
            await await bus.CommandAsync(new CreateAccount("Account", AccountType.Saving));

            var gbp = new Currency("GBP");
           
            await bus.Command(new CreateTransaction("Tx", new Quantity(100, gbp), Transaction.TransactionType.General, string.Empty));
            await bus.Command(new CreateTransaction("Tx2", new Quantity(-100, gbp), Transaction.TransactionType.General, string.Empty));

            await bus.Command(new AddTransaction("Account", "Tx"));
            await bus.Command(new AddTransaction("Account", "Tx2"));

            var list = await bus.QueryAsync(new TransactionListQuery("Account") { IncludeInfo = false });
            Assert.Empty(list.Infos);
            
            list = await bus.QueryUntil(new TransactionListQuery("Account"), r => r.TxId.Count > 0);
            Assert.Contains("Tx", list.TxId);
            Assert.Contains("Tx2", list.TxId);
            
            var tx = list.Infos.Single(t => t.TxId == "Tx");
            Assert.Equal(100, tx.Quantity.Amount);
            Assert.Equal(gbp, tx.Quantity.Denominator);
            Assert.Equal(Transaction.TransactionType.General, tx.TransactionType);
            Assert.Equal(string.Empty, tx.Comment);
            Assert.Null(tx.AssetId);
            
            var tx2 = list.Infos.Single(t => t.TxId == "Tx2");
            Assert.Equal(-100, tx2.Quantity.Amount);
            Assert.Equal(gbp, tx2.Quantity.Denominator);
            Assert.Equal(Transaction.TransactionType.General, tx2.TransactionType);
            Assert.Equal(string.Empty, tx2.Comment);
            Assert.Null(tx2.AssetId);
            
            await bus.Equal(new AccountStatsQuery("Account", gbp), a => a.Balance, new Quantity(0, gbp));
        }

        [Fact]
        public async Task CanGetBalanceInUsd()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();
            var webQuoteApi = webApiProvider.GetQuoteApi(AssetType.Currency, AssetType.Currency, false);

            await connector.SetAsync(webSearchApi.GetUrl("GBPUSD"), 
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3234,\"previousCloseDate\":\"2026-04-06\"}]");

            var date = new LocalDateTime(2023, 10, 9, 12, 30).InUtc().ToInstant().ToTime();

            await connector.SetAsync(webQuoteApi.GetUrl("GBPUSD.FOREX", date),
                "[{\"date\":\"2023-10-09\",\"open\":1.222,\"high\":1.2252,\"low\":1.2163,\"close\":1.225,\"adjusted_close\":1.225,\"volume\":483}]");

            var gbp = new Currency("GBP");
            var usd = new Currency("USD");

            await await bus.CommandAsync(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(gbp, usd), gbp, usd), date));

            var account = "Bank";
            await await bus.CommandAsync(new RetroactiveCommand<CreateAccount>(new CreateAccount(account, AccountType.Saving), date));

            var txId = "ATM";
            await await bus.CommandAsync(new RetroactiveCommand<CreateTransaction>(new CreateTransaction(txId, new Quantity(100, gbp), Transaction.TransactionType.General, null), date));

            await await bus.CommandAsync(new RetroactiveCommand<AddTransaction>(new AddTransaction(account, txId), date));
            
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
                    await await bus.CommandAsync(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(fordom), t.Date.ToTime()));
            }

            var balance = await bus.QueryAsync(new HistoricalQuery<AccountStatsQuery,AccountStats>(new AccountStatsQuery(account, usd), date));
            Assert.NotNull(balance);
            Assert.NotNull(balance.Balance.Denominator);
        }

        [Fact]
        public async Task CanGetAssetTriangulationPrice()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var repository = container.GetInstance<IEsRepository<IAggregate>>();
            var timeline = container.GetInstance<ITimeline>();
          
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();

            await connector.SetAsync(webSearchApi.GetUrl("GBPUSD"), 
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3234,\"previousCloseDate\":\"2026-04-06\"}]");
            await connector.SetAsync(webSearchApi.GetUrl("Bitcoin-USD"), 
                "[{\"Code\":\"BTC-USD\",\"Exchange\":\"CC\",\"Name\":\"Bitcoin\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":68686.0390625,\"previousCloseDate\":\"2026-04-07\"}]");
            
            var usd = new Currency("USD");
            var gbp = new Currency("GBP"); 
            var asset = new Asset("Bitcoin", AssetType.Coin);
            await await bus.CommandAsync(new RegisterAssetPair(AssetPair.Fordom(asset, usd), asset, usd));
            await await bus.CommandAsync(new AddQuote(AssetPair.Fordom(asset, usd), timeline.Now.ToInstant(), 23000));

            await await bus.CommandAsync(new RegisterAssetPair(AssetPair.Fordom(gbp, usd), gbp, usd));
            await await bus.CommandAsync(new AddQuote(AssetPair.Fordom(gbp, usd), timeline.Now.ToInstant(), 1.3));
            
            await await bus.CommandAsync(new CreateAccount("Account", AccountType.Trading));
            await await bus.CommandAsync(new DepositAsset("Account", new Quantity(1.0, asset)));
            
            await bus.Equal(new AccountStatsQuery("Account", gbp), s => s.Balance, new Quantity(23000 / 1.3, gbp));
        }

        [Fact]
        public async Task CanProcessHashflare()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            var manager = container.GetInstance<IBranchManager>();

            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();

            await connector.SetAsync(webSearchApi.GetUrl("Bitcoin-USD"), 
                "[{\"Code\":\"BTC-USD\",\"Exchange\":\"CC\",\"Name\":\"Bitcoin\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":68686.0390625,\"previousCloseDate\":\"2026-04-07\"}]");
            
            var asset = new Asset("Bitcoin", AssetType.Coin);
            var usd = new Currency("USD");
            await bus.Command(new RegisterAssetPair(AssetPair.Fordom(asset, usd), asset, usd));
            await bus.Command(new AddQuote(AssetPair.Fordom(asset, usd), timeline.Now.ToInstant(), 23000));
            
            await bus.Command(new RegisterHashflare("user@mail.com"));
            await manager.Ready;
            await bus.Command(new CreateContract("0", asset.AssetId, 100, 1000));
            await bus.Command(new AddMinedCoinToHashflare(asset.AssetId, 0.1));

            await bus.Equal(new AccountStatsQuery("Hashflare", usd), a => a.Balance, new Quantity(23000 * 0.1, usd));
        }
    }
}