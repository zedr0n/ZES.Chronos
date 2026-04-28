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
        public async Task CanSpendAsset()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var log = container.GetInstance<ILog>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            
            var webSearchApi = webApiProvider.GetSearchApi();
            var coinQuoteApi = webApiProvider.GetQuoteApi(AssetType.Coin, AssetType.Currency, false);
            var fxQuoteApi = webApiProvider.GetQuoteApi(AssetType.Currency, AssetType.Currency, false); 
           
            var date = new LocalDateTime(2017, 8, 16, 12, 30).InUtc().ToInstant().ToTime();
            
            await bus.Command(new RetroactiveCommand<CreateAccount>(new CreateAccount("Account", AccountType.Trading), date));
            
            var btc = new Asset("BTC", AssetType.Coin);
            var gbp = new Currency("GBP");
            var usd = new Currency("USD");
            
            await connector.SetAsync(webSearchApi.GetUrl(fxQuoteApi.GetSearchTicker(gbp, usd)),
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3537,\"previousCloseDate\":\"2026-04-27\"}]");
            await connector.SetAsync(webSearchApi.GetUrl(coinQuoteApi.GetSearchTicker(btc, usd)),
                "[{\"Code\":\"BTC-USD\",\"Exchange\":\"CC\",\"Name\":\"Bitcoin\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":76734.7109375,\"previousCloseDate\":\"2026-04-28\"}]");
 
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(gbp, usd), Time.MinValue)); 
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(btc, usd), Time.MinValue)); 

            var assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(btc, usd)), date));
            var btcTicker = assetPairInfo.Ticker;
            
            assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(gbp, usd)), date));
            var gbpTicker = assetPairInfo.Ticker;

            await connector.SetAsync(coinQuoteApi.GetUrl(btcTicker, date),
                "[{\"date\":\"2017-08-16\",\"open\":4200.33984375,\"high\":4381.2299804688,\"low\":3994.419921875,\"close\":4376.6298828125,\"adjusted_close\":4376.6298828125,\"volume\":2272039936}]");
            await connector.SetAsync(fxQuoteApi.GetUrl(gbpTicker, date),
                "[{\"date\":\"2017-08-16\",\"open\":1.2868,\"high\":1.2903,\"low\":1.2845,\"close\":1.2867,\"adjusted_close\":1.2867,\"volume\":474}]");
            
            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("Account",new Quantity(0.06, btc), new Quantity(241.58, usd)), date));
            await bus.Command(new RetroactiveCommand<SpendAsset>(new SpendAsset("Account",new Quantity(0.00114549, btc), new Quantity(double.NaN, usd)), date));
            
            var stats = await bus.QueryAsync(new AccountStatsQuery("Account", gbp) { QueryNet = true, Timestamp = date });
            log.Info(stats);
        }

        [Fact]
        public async Task CanComputeSameDayDisposal()
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

            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("Account",new Quantity(100, asset), new Quantity(7.54*100, ccy)), date));
            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("Account",new Quantity(-50, asset), new Quantity(-7.19*50, ccy)), date));
            
            var stats = await bus.QueryAsync(new AccountStatsQuery("Account", ccy));
            Assert.Equal((-7.54+7.19)*50, stats.RealisedGains[0].Amount, 1e-6);
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
            Assert.Equal((-7.54+7.19)*50, stats.RealisedGains[0].Amount, 1e-6);
        }

        [Fact]
        public async Task CanComputeCostBasisForAssetSwaps()
        { 
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            
            var webSearchApi = webApiProvider.GetSearchApi();
            var fxQuoteApi = webApiProvider.GetQuoteApi(AssetType.Currency, AssetType.Currency, false);
            var coinQuoteApi = webApiProvider.GetQuoteApi(AssetType.Coin, AssetType.Currency, false);
            
            await bus.Command(new RetroactiveCommand<CreateAccount>(new CreateAccount("Account", AccountType.Trading), Time.MinValue));
            
            var btc = new Asset("BTC", AssetType.Coin);
            var eth = new Asset("ETH", AssetType.Coin);
            var gbp = new Currency("GBP");
            var usd = new Currency("USD");
            
            await connector.SetAsync(webSearchApi.GetUrl("BTC-USD"),
                "[{\"Code\":\"BTC-USD\",\"Exchange\":\"CC\",\"Name\":\"Bitcoin\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":78075.7109375,\"previousCloseDate\":\"2026-04-24\"}]");
            await connector.SetAsync(webSearchApi.GetUrl("ETH-USD"),
                "[{\"Code\":\"ETH-USD\",\"Exchange\":\"CC\",\"Name\":\"Ethereum\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":2318.5300292969,\"previousCloseDate\":\"2026-04-24\"}]");
            await connector.SetAsync(webSearchApi.GetUrl("GBPUSD"),
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3466,\"previousCloseDate\":\"2026-04-23\"}]");
 
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(btc, usd), Time.MinValue));
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(eth, usd), Time.MinValue));
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(gbp, usd), Time.MinValue));
            
            var date = new LocalDateTime(2016, 12, 30, 12, 30).InUtc().ToInstant().ToTime();
            var date2 = new LocalDateTime(2017, 8, 14, 12, 30).InUtc().ToInstant().ToTime();
           
            var assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(gbp, usd)), date));
            var ticker = assetPairInfo.Ticker;
            await connector.SetAsync(fxQuoteApi.GetUrl(ticker, date),
                "[{\"date\":\"2016-12-30\",\"open\":1.2285,\"high\":1.2387,\"low\":1.227,\"close\":1.2288,\"adjusted_close\":1.2288,\"volume\":965}]");
            await connector.SetAsync(fxQuoteApi.GetUrl(ticker, date2),
                "[{\"date\":\"2017-08-14\",\"open\":1.3008,\"high\":1.3023,\"low\":1.2961,\"close\":1.3006,\"adjusted_close\":1.3006,\"volume\":493}]");
            
            assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(btc, usd)), date));
            ticker = assetPairInfo.Ticker;
            await connector.SetAsync(coinQuoteApi.GetUrl(ticker, date2),
                "[{\"date\":\"2017-08-14\",\"open\":4066.1000976563,\"high\":4325.1298828125,\"low\":3989.1599121094,\"close\":4325.1298828125,\"adjusted_close\":4325.1298828125,\"volume\":2463089920}]");
            await connector.SetAsync(coinQuoteApi.GetPreciseUrl(ticker, date2),
                "[{\"date\":\"2017-08-14\",\"open\":4066.1000976563,\"high\":4325.1298828125,\"low\":3989.1599121094,\"close\":4325.1298828125,\"adjusted_close\":4325.1298828125,\"volume\":2463089920}]");
            
            assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(eth, usd)), date));
            ticker = assetPairInfo.Ticker;
            await connector.SetAsync(coinQuoteApi.GetUrl(ticker, date2),
                "[{\"date\":\"2017-08-14\",\"open\":298.0310058594,\"high\":306.8070068359,\"low\":296.4119873047,\"close\":300.0969848633,\"adjusted_close\":300.0969848633,\"volume\":864390976}]");
            await connector.SetAsync(coinQuoteApi.GetPreciseUrl(ticker, date2),
                "[{\"date\":\"2017-08-14\",\"open\":298.0310058594,\"high\":306.8070068359,\"low\":296.4119873047,\"close\":300.0969848633,\"adjusted_close\":300.0969848633,\"volume\":864390976}]");
            
            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("Account",new Quantity(0.2, btc), new Quantity(166, gbp)), date));
            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("Account",new Quantity(0.05, btc), new Quantity(0.05*4325.12988281251/1.3006, gbp)), date2));
            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("Account",new Quantity(0.35, eth), new Quantity(0.025, btc)), date2));
            
            var stats = await bus.QueryAsync(new AccountStatsQuery("Account", usd) { QueryNet = true, Timestamp = date2 });
            Assert.Equal(166*1.2288+0.05*4325.1298828125*0.5, stats.CostBasis[0].Amount, 1e-6);
            Assert.Equal(0, stats.RealisedGains[0].Amount, 1e-6);
            Assert.Equal(0.025*4325.12988281251, stats.CostBasis[1].Amount, 1e-6);
            Assert.Equal(0, stats.RealisedGains[1].Amount);
        }

        [Fact]
        public async Task CanComputeCostBasisWithTransfers()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();
            var equityQuoteApi = webApiProvider.GetQuoteApi(AssetType.Equity, AssetType.Currency, false);

            var iukd = new Asset("IUKD", AssetType.Equity);
            var gbp = new Currency("GBP");
            var gbx = new Currency("GBX");

            await connector.SetAsync(webSearchApi.GetUrl(iukd.AssetId),
                "[{\"Code\":\"IUKD\",\"Exchange\":\"LSE\",\"Name\":\"iShares UK Dividend UCITS\",\"Type\":\"ETF\",\"Country\":\"UK\",\"Currency\":\"GBX\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":944,\"previousCloseDate\":\"2026-03-25\"},{\"Code\":\"IUKD\",\"Exchange\":\"SW\",\"Name\":\"iShares UK Dividend UCITS ETF GBP (Dist) CHF\",\"Type\":\"ETF\",\"Country\":\"Switzerland\",\"Currency\":\"CHF\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":9.948,\"previousCloseDate\":\"2026-03-25\"}]");
            
            var date = new LocalDateTime(2021, 8, 26, 12, 30).InUtc().ToInstant().ToTime();
            var date2 = new LocalDateTime(2022, 8, 26, 12, 30).InUtc().ToInstant().ToTime();
           
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(gbp, gbx), date));
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(iukd, gbx), date));
            
            await bus.Command(new RetroactiveCommand<CreateAccount>(new CreateAccount("Old", AccountType.Trading), date));
            await bus.Command(new RetroactiveCommand<CreateAccount>(new CreateAccount("New", AccountType.Trading), date));
           
            var assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(iukd, gbx)), date));
            var ticker = assetPairInfo.Ticker;
            await connector.SetAsync(equityQuoteApi.GetUrl(ticker, date),
                "[{\"date\":\"2021-08-26\",\"open\":751.7,\"high\":754.4,\"low\":750.814,\"close\":751.9,\"adjusted_close\":577.6913,\"volume\":163286}]");
            await connector.SetAsync(equityQuoteApi.GetUrl(ticker, date2),
                "[{\"date\":\"2022-08-26\",\"open\":724.1,\"high\":729.7,\"low\":720.1,\"close\":720.1,\"adjusted_close\":589.2986,\"volume\":97436}]");
            
            var price = await bus.QueryAsync(new AssetQuoteQuery(iukd, gbp) { UpdateQuote = true, Timestamp = date });
            var price2 = await bus.QueryAsync(new AssetQuoteQuery(iukd, gbp) { UpdateQuote = true, Timestamp = date2 });
           
            await bus.Command(new RetroactiveCommand<CreateTransaction>( new CreateTransaction("Tx", price.Quantity*100, Transaction.TransactionType.Transfer, "Transfer"), date));
            await bus.Command(new RetroactiveCommand<AddTransaction>(new AddTransaction("Old", "Tx"), date));
            
            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("Old", new Quantity(100, iukd), price.Quantity*100), date));
            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("Old", new Quantity(-60, iukd), price2.Quantity*(-60)), date2));
            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("Old", new Quantity(10, iukd), price2.Quantity*10), date2));
            
            var stats = await bus.QueryAsync(new AccountStatsQuery("Old", new Currency("GBP")) { QueryNet = true, Timestamp = date2 });
            Assert.Equal(price.Quantity.Amount*100 -  price.Quantity.Amount*50, stats.CostBasis[0].Amount, 1e-4);
            Assert.Equal(price2.Quantity.Amount*50 - price.Quantity.Amount*50, stats.RealisedGains[0].Amount, 1e-4);

            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("New", new Quantity(50, iukd), price2.Quantity*50), date2));
            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("New", new Quantity(-25, iukd), price2.Quantity*(-25)), date2));
            
            stats = await bus.QueryAsync(new AccountStatsQuery("New", new Currency("GBP")) { QueryNet = true, Timestamp = date2 });
            Assert.Equal(price2.Quantity.Amount*25, stats.CostBasis[0].Amount, 1e-4);
            
            await bus.Command(new RetroactiveCommand<StartTransfer>(new StartTransfer("Transfer", "Old", "New", new Quantity(25, iukd)), date2));
            stats = await bus.QueryAsync(new AccountStatsQuery("Old", new Currency("GBP")) { QueryNet = true, Timestamp = date2 });
            Assert.Equal(price.Quantity.Amount*25, stats.CostBasis[0].Amount, 1e-4);
            Assert.Equal((price2.Quantity.Amount - price.Quantity.Amount)*25, stats.RealisedGains[0].Amount, 1e-4);
            
            stats = await bus.QueryAsync(new AccountStatsQuery("New", new Currency("GBP")) { QueryNet = true, Timestamp = date2 });
            // the transferred disposals ( -25 ) will match with current acquisitions ( 50 - 25 = 25 ) so we'll be left just with section 104 cost basis
            Assert.Equal(50*7.519, stats.CostBasis[0].Amount, 1e-4);
            
        }

        [Fact]
        public async Task CanComputeIrr()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();

            var date = new LocalDateTime(2021, 8, 26, 12, 30).InUtc().ToInstant().ToTime();
            var date2 = new LocalDateTime(2022, 8, 26, 12, 30).InUtc().ToInstant().ToTime();
            var date3 = new LocalDateTime(2023, 8, 26, 12, 30).InUtc().ToInstant().ToTime();
            
            var interestRate = 0.04;
            var interestRate2 = 0.06;
            
            await bus.Command(new RetroactiveCommand<CreateAccount>(new CreateAccount("Account", AccountType.Saving), date));
            await bus.Command(new RetroactiveCommand<CreateTransaction>(new CreateTransaction("Tx", new Quantity(20000, new Currency("GBP")), Transaction.TransactionType.Transfer, "Transfer In"), date));
            await bus.Command(new RetroactiveCommand<AddTransaction>(new AddTransaction("Account", "Tx"), date));
            
            await bus.Command(new RetroactiveCommand<CreateTransaction>(new CreateTransaction("Tx2", new Quantity(20000*interestRate, new Currency("GBP")), Transaction.TransactionType.Interest, "Interest"), date2));
            await bus.Command(new RetroactiveCommand<AddTransaction>(new AddTransaction("Account", "Tx2"), date2));
            
            await bus.Command(new RetroactiveCommand<CreateTransaction>(new CreateTransaction("Tx3", new Quantity(20000*(1+interestRate)*interestRate2, new Currency("GBP")), Transaction.TransactionType.Interest, "Interest"), date3));
            await bus.Command(new RetroactiveCommand<AddTransaction>(new AddTransaction("Account", "Tx3"), date3));
            
            await bus.Command(new RetroactiveCommand<CreateTransaction>(new CreateTransaction("Tx4", new Quantity(-20000*(1+interestRate)*(1+interestRate2), new Currency("GBP")), Transaction.TransactionType.Transfer, "Transfer Out"), date3));
            await bus.Command(new RetroactiveCommand<AddTransaction>(new AddTransaction("Account", "Tx4"), date3));

            var stats = await bus.QueryAsync(new AccountStatsQuery("Account", new Currency("GBP")) { Timestamp = date3 });
            Assert.Equal(0.05, stats.Irr, 1e-3);
            
            var blendedIrr = await bus.QueryAsync(new BlendedIrrQuery(["Account"], new Currency("GBP")) { Timestamp = date3 });
            Assert.Equal(0.05, blendedIrr.Irr, 1e-3);
            
            var intIrr = await bus.QueryAsync(new BlendedIrrQuery(["Account"], new Currency("GBP")) { Timestamp = date3, Start = date2.ToInstant() });
            Assert.Equal(0.06, intIrr.Irr, 1e-3);
        }

        [Fact]
        public async Task CanComputeIrrForAssetTransfers()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();

            var iukd = new Asset("IUKD", AssetType.Equity);
            var gbp = new Currency("GBP");
            var gbx = new Currency("GBX");
            
            var date = new LocalDateTime(2021, 8, 26, 12, 30).InUtc().ToInstant().ToTime();
            var date2 = new LocalDateTime(2022, 8, 26, 12, 30).InUtc().ToInstant().ToTime();
           
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(gbp, gbx), date));
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(iukd, gbx), date));
            
            await bus.Command(new RetroactiveCommand<CreateAccount>(new CreateAccount("Old", AccountType.Trading), date));
            await bus.Command(new RetroactiveCommand<CreateAccount>(new CreateAccount("New", AccountType.Trading), date));
            
            var price = await bus.QueryAsync(new AssetQuoteQuery(iukd, gbp) { UpdateQuote = true, Timestamp = date });
           
            await bus.Command(new RetroactiveCommand<CreateTransaction>( new CreateTransaction("Tx", price.Quantity*100, Transaction.TransactionType.Transfer, "Transfer"), date));
            await bus.Command(new RetroactiveCommand<AddTransaction>(new AddTransaction("Old", "Tx"), date));
            
            await bus.Command(new RetroactiveCommand<TransactAsset>(new TransactAsset("Old", new Quantity(100, iukd), price.Quantity*100), date));
            
            var stats = await bus.QueryAsync(new AccountStatsQuery("Old", new Currency("GBP")) { QueryNet = true, Timestamp = date2 });
            Assert.Equal(-0.0423, stats.Irr, 1e-4);
            
            await bus.Command(new RetroactiveCommand<StartTransfer>(new StartTransfer("Transfer", "Old", "New", new Quantity(100, iukd)), date2));
            stats = await bus.QueryAsync(new AccountStatsQuery("Old", new Currency("GBP")) { QueryNet = true, Timestamp = date2 });
            Assert.Equal(-0.0423, stats.Irr, 1e-4);
            stats = await bus.QueryAsync(new AccountStatsQuery("New", new Currency("GBP")) { QueryNet = true, Timestamp = date2 });
            Assert.Equal(0, stats.Irr, 1e-4);
            
            var blenderIrr = await bus.QueryAsync(new BlendedIrrQuery(["Old", "New"], new Currency("GBP")) { Timestamp = date2 });
            Assert.Equal(-0.0423, blenderIrr.Irr, 1e-4);
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