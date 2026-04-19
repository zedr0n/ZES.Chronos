using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chronos.Accounts;
using Chronos.Accounts.Commands;
using Chronos.Accounts.Queries;
using Chronos.Core;
using Chronos.Core.Commands;
using Chronos.Core.Net;
using Chronos.Core.Queries;
using NodaTime;
using Xunit;
using ZES.Infrastructure;
using ZES.Infrastructure.Alerts;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Net;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Infrastructure;
using ZES.Interfaces.Net;
using ZES.TestBase;
using IClock = ZES.Interfaces.Clocks.IClock;

namespace Chronos.Tests
{
    public class AssetTests : ChronosTest
    {
        public AssetTests(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
        }

        [Fact]
        public async Task CanRecordTransaction()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();
            
            var gbp = new Currency("GBP");
            var usd = new Currency("USD");
            
            await connector.SetAsync(webSearchApi.GetUrl("GBPUSD"),
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3335,\"previousCloseDate\":\"2026-03-26\"}]");

            await bus.Command(new RegisterAssetPair("GBPUSD", gbp, usd));
            await bus.Command(new AddQuote("GBPUSD", timeline.Now.ToInstant(), 1.2));

            await bus.Command(new CreateTransaction("Tx", new Quantity(100, gbp), Transaction.TransactionType.General, string.Empty));
            await bus.Equal(new TransactionInfoQuery("Tx", usd), t => t.Quantity.Amount, 100 * 1.2);
        }

        [Fact]
        public async Task CanGetDividendInfo()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();
            var eqQuoteApi = webApiProvider.GetQuoteApi(AssetType.Equity, AssetType.Currency, true);

            var domAsset = new Asset("GBP", AssetType.Currency);
            var quoteAsset = new Currency("GBX");
            var iukdAsset = new Asset("IUKD", AssetType.Equity);
            
            await connector.SetAsync(webSearchApi.GetUrl(iukdAsset.AssetId),
                "[{\"Code\":\"IUKD\",\"Exchange\":\"LSE\",\"Name\":\"iShares UK Dividend UCITS\",\"Type\":\"ETF\",\"Country\":\"UK\",\"Currency\":\"GBX\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":944,\"previousCloseDate\":\"2026-03-25\"},{\"Code\":\"IUKD\",\"Exchange\":\"SW\",\"Name\":\"iShares UK Dividend UCITS ETF GBP (Dist) CHF\",\"Type\":\"ETF\",\"Country\":\"Switzerland\",\"Currency\":\"CHF\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":9.948,\"previousCloseDate\":\"2026-03-25\"}]");

            await connector.SetAsync(eqQuoteApi.GetUrl("IUKD.LSE", null, true),
                "{\"code\":\"IUKD.LSE\",\"timestamp\":1776440100,\"gmtoffset\":0,\"open\":995.4,\"high\":999.4,\"low\":989.4,\"close\":999.4,\"volume\":329826,\"previousClose\":993.6,\"change\":5.8,\"change_p\":0.5837}");
            
            await bus.Command(new RegisterAssetPair(AssetPair.Fordom(domAsset, quoteAsset), domAsset, quoteAsset));
            await bus.Command(new RegisterAssetPair(AssetPair.Fordom(iukdAsset, quoteAsset), iukdAsset, quoteAsset));
            await bus.Command(new CreateAccount("Main", AccountType.Trading));

            await bus.Command(new TransactAsset("Main", new Quantity(100, iukdAsset), new Quantity(9.994*100, domAsset)));
            await bus.Command(new CreateTransaction("Dividend", new Quantity(1, domAsset), Transaction.TransactionType.Dividend, "Dividend", iukdAsset.AssetId));
            await bus.Command(new AddTransaction("Main", "Dividend"));
           
            await bus.Command(new UpdateQuote(AssetPair.Fordom(iukdAsset, quoteAsset)) { EnforceCache = true });
            
            //var res = await bus.QueryAsync(new TransactionInfoQuery("Dividend", domAsset));
            var res = await bus.QueryAsync(new AccountStatsQuery("Main", domAsset));
            Assert.Equal(1, res.Balance.Amount);
            Assert.Equal(1-9.994*100, res.CashBalance.Amount);
            Assert.Equal(1, res.Dividends.Sum(x => x?.Amount ?? 0));
        }

        [Fact]
        public async Task CanUseTransactionQuote()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();
            
            var gbp = new Currency("GBP");
            var usd = new Currency("USD");

            await connector.SetAsync(webSearchApi.GetUrl("GBPUSD"),
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3335,\"previousCloseDate\":\"2026-03-26\"}]");

            await bus.Command(new RegisterAssetPair("GBPUSD", gbp, usd));
            await bus.Command(new AddQuote("GBPUSD", timeline.Now.ToInstant(), 1.2));

            await bus.Command(new CreateTransaction("Tx", new Quantity(100, gbp), Transaction.TransactionType.General, string.Empty));
            await bus.Command(new AddTransactionQuote("Tx", new Quantity(110, usd)));
            await bus.Equal(new TransactionInfoQuery("Tx", usd), t => t.Quantity.Amount, 110);
        }

        [Fact]
        public async Task CanUseCurrencyPair()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();

            var forAsset = new Currency("GBP");
            var domAsset = new Currency("USD");

            await connector.SetAsync(webSearchApi.GetUrl("GBPUSD"),
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3335,\"previousCloseDate\":\"2026-03-26\"}]");
            await bus.Command(new RegisterAssetPair("GBPUSD", forAsset, domAsset));
            await bus.Command(new AddQuote("GBPUSD", timeline.Now.ToInstant(), 1.2));

            var assetsInfo = await bus.QueryAsync(new AssetPairsInfoQuery());
            Assert.Contains(forAsset, assetsInfo.Assets);
            Assert.Contains(domAsset, assetsInfo.Assets);
        }

        [Fact]
        public async Task CanRegisterCurrencyAndAsset()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();

            var forAsset = new Asset("USD", AssetType.Currency);
            var domAsset = new Asset("GBP", AssetType.Currency);
            
            var forCcy = new Currency("USD");
            var domCcy = new Currency("GBP");
            
            Assert.True(forAsset.Equals(forCcy));
            Assert.True(domAsset.Equals(domCcy));
            Assert.True(forAsset == forCcy);
            Assert.True(domAsset == domCcy);
            
            await connector.SetAsync(webSearchApi.GetUrl("USDGBP"),
                "[{\"Code\":\"USDGBP\",\"Exchange\":\"FOREX\",\"Name\":\"US Dollar\\/UK Pound Sterling FX Cross Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"GBP\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":0.7546,\"previousCloseDate\":\"2026-03-31\"}]");
            
            await bus.Command(new RegisterAssetPair("USDGBP", forAsset, domAsset));
            
            var assetPairsInfo = await bus.QueryAsync(new AssetPairsInfoQuery());
            Assert.Equal(2, assetPairsInfo.Assets.Length);
            Assert.Equal(2, assetPairsInfo.Tree.VertexCount);
        }

        [Fact]
        public async Task CanGetTicker()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();

            var date = new LocalDateTime(2020, 12, 1, 12, 30).InUtc().ToInstant().ToTime();

            var forAsset = new Currency("GBP");
            var domAsset = new Currency("USD");

            await connector.SetAsync(webSearchApi.GetUrl("GBPUSD"),
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3335,\"previousCloseDate\":\"2026-03-26\"}]");
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair("GBPUSD", forAsset, domAsset), date));
            await bus.Command(new RetroactiveCommand<AddQuoteTicker>(new AddQuoteTicker("GBPUSD", "GBPUSD.FOREX"), date));
            
            var assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(forAsset, domAsset)), date));
            var ticker = assetPairInfo.Ticker;
            
            Assert.Equal("GBPUSD.FOREX", ticker);
        }

        [Fact]
        public async Task CanGetEphemeralQuote()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();
            var fxQuoteApi = webApiProvider.GetQuoteApi(AssetType.Currency, AssetType.Currency, false);
            
            var forAsset = new Asset("GBP", AssetType.Currency);
            var domAsset = new Asset("USD", AssetType.Currency);
           
            var date = new LocalDateTime(2020, 12, 1, 12, 30).InUtc().ToInstant().ToTime();
            var laterDate = new LocalDateTime(2021, 12, 2, 12, 30).InUtc().ToInstant().ToTime();
            var endDate = new LocalDateTime(2022, 12, 2, 12, 30).InUtc().ToInstant().ToTime();
            
            await connector.SetAsync(webSearchApi.GetUrl("GBPUSD"),
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3335,\"previousCloseDate\":\"2026-03-26\"}]");
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair("GBPUSD", forAsset, domAsset), date));
            
            var assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(forAsset, domAsset)), date));
            var ticker = assetPairInfo.Ticker;
            
            await connector.SetAsync(fxQuoteApi.GetUrl(ticker, date),
                "[{\"date\":\"2020-12-01\",\"open\":1.3337,\"high\":1.3439,\"low\":1.3319,\"close\":1.3337,\"adjusted_close\":1.3337,\"volume\":908}]");
            await connector.SetAsync(fxQuoteApi.GetUrl(ticker, laterDate),
                "[{\"date\":\"2021-12-02\",\"open\":1.3279,\"high\":1.3333,\"low\":1.3273,\"close\":1.328,\"adjusted_close\":1.328,\"volume\":470}]");
            await connector.SetAsync(fxQuoteApi.GetUrl(ticker, endDate),
                "[{\"date\":\"2022-12-02\",\"open\":1.2259,\"high\":1.23,\"low\":1.2135,\"close\":1.2293,\"adjusted_close\":1.2293,\"volume\":1033}]");
            
            var updateQuote = new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(forAsset, domAsset)) { Ephemeral = true }, date ) ;
            await bus.Command(updateQuote);
            
            var updateQuoteLater = new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(forAsset, domAsset)) { Ephemeral = true }, laterDate ) ;
            await bus.Command(updateQuoteLater);
            
            var updateQuoteEnd = new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(forAsset, domAsset)), endDate ) ;
            await bus.Command(updateQuoteEnd);
 
            var res = await bus.QueryAsync(new HistoricalQuery<AssetQuoteQuery,AssetQuote>(new AssetQuoteQuery(forAsset, domAsset), date));
            Assert.Equal(1.3337, res.Quantity.Amount);
            
            res = await bus.QueryAsync(new HistoricalQuery<AssetQuoteQuery,AssetQuote>(new AssetQuoteQuery(forAsset, domAsset), laterDate));
            Assert.Equal(1.328, res.Quantity.Amount);
            
            res = await bus.QueryAsync(new HistoricalQuery<AssetQuoteQuery,AssetQuote>(new AssetQuoteQuery(forAsset, domAsset), endDate));
            Assert.Equal(1.2293, res.Quantity.Amount);
        }

        [Fact]
        public async Task CanUpdateQuoteDuringQuery()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();
            var fxQuoteApi = webApiProvider.GetQuoteApi(AssetType.Currency, AssetType.Currency, false);

            var forAsset = new Asset("GBP", AssetType.Currency);
            var domAsset = new Asset("USD", AssetType.Currency);
            
            var date = new LocalDateTime(2020, 12, 1, 12, 30).InUtc().ToInstant().ToTime();
            var laterDate = new LocalDateTime(2021, 12, 2, 12, 30).InUtc().ToInstant().ToTime();

            await connector.SetAsync(webSearchApi.GetUrl(AssetPair.Fordom(forAsset, domAsset)),
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3335,\"previousCloseDate\":\"2026-03-26\"}]");
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair("GBPUSD", forAsset, domAsset), date));
            
            var assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(forAsset, domAsset)), date));
            var ticker = assetPairInfo.Ticker;
            
            await connector.SetAsync(fxQuoteApi.GetUrl(ticker, date),
                "[{\"date\":\"2020-12-01\",\"open\":1.3337,\"high\":1.3439,\"low\":1.3319,\"close\":1.3337,\"adjusted_close\":1.3337,\"volume\":908}]");
            await connector.SetAsync(fxQuoteApi.GetUrl(ticker, laterDate),
                "[{\"date\":\"2021-12-02\",\"open\":1.3279,\"high\":1.3333,\"low\":1.3273,\"close\":1.328,\"adjusted_close\":1.328,\"volume\":470}]");

            var res = await bus.QueryAsync(new HistoricalQuery<AssetQuoteQuery,AssetQuote>(new AssetQuoteQuery(forAsset, domAsset) { UpdateQuote = true }, date));
            Assert.Equal(1.3337, res.Quantity.Amount);
            
            res = await bus.QueryAsync(new HistoricalQuery<AssetQuoteQuery,AssetQuote>(new AssetQuoteQuery(forAsset, domAsset) { UpdateQuote = true }, date));
            Assert.Equal(1.3337, res.Quantity.Amount);
            
            res = await bus.QueryAsync(new HistoricalQuery<AssetQuoteQuery,AssetQuote>(new AssetQuoteQuery(forAsset, domAsset) { UpdateQuote = true }, laterDate));
            Assert.Equal(1.328, res.Quantity.Amount);
           
            assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(forAsset, domAsset)), laterDate));
            Assert.Equal(2, assetPairInfo.QuoteDates.Length);
        }

        [Fact]
        public async Task CanQuerySameWorkingDayQuote()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();
            var fxQuoteApi = webApiProvider.GetQuoteApi(AssetType.Currency, AssetType.Currency, false);

            var forAsset = new Asset("GBP", AssetType.Currency);
            var domAsset = new Asset("USD", AssetType.Currency);
            
            var date = new LocalDateTime(2020, 12, 1, 18, 30).InUtc().ToInstant().ToTime();
            var otherDate = new LocalDateTime(2020, 12, 1, 12, 30).InUtc().ToInstant().ToTime();
            
            await connector.SetAsync(webSearchApi.GetUrl(AssetPair.Fordom(forAsset, domAsset)),
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3335,\"previousCloseDate\":\"2026-03-26\"}]");
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair("GBPUSD", forAsset, domAsset), Time.MinValue));
            
            var assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(forAsset, domAsset)), date));
            var ticker = assetPairInfo.Ticker;
           
            await connector.SetAsync(fxQuoteApi.GetUrl(ticker, date),
                "[{\"date\":\"2020-12-01\",\"open\":1.3337,\"high\":1.3439,\"low\":1.3319,\"close\":1.3337,\"adjusted_close\":1.3337,\"volume\":908}]");
           
            var res = await bus.QueryAsync(new HistoricalQuery<AssetQuoteQuery,AssetQuote>(new AssetQuoteQuery(forAsset, domAsset) { UpdateQuote = true }, date));
            Assert.Equal(1.3337, res.Quantity.Amount);
            
            res = await bus.QueryAsync(new HistoricalQuery<AssetQuoteQuery,AssetQuote>(new AssetQuoteQuery(forAsset, domAsset) { UpdateQuote = true }, otherDate));
            Assert.Equal(1.3337, res.Quantity.Amount);
        }

        [Fact]
        public async Task CanApplyStockSplit()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var log = container.GetInstance<ILog>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();
           
            var date = new LocalDateTime(2023, 12, 1, 12, 30).InUtc().ToInstant().ToTime();
            
            var domAsset = new Currency("USD");
            var forAsset = new Asset("CORN", AssetType.Equity);
           
            var eqQuoteApi = webApiProvider.GetQuoteApi(forAsset.AssetType, domAsset.AssetType, false);

            await connector.SetAsync(webSearchApi.GetUrl("CORN"),
                "[{\"Code\":\"CORN\",\"Exchange\":\"US\",\"Name\":\"Teucrium Corn Fund\",\"Type\":\"ETF\",\"Country\":\"USA\",\"Currency\":\"USD\",\"ISIN\":\"US88166A1025\",\"isPrimary\":false,\"previousClose\":17.77,\"previousCloseDate\":\"2026-04-13\"},{\"Code\":\"CORN\",\"Exchange\":\"LSE\",\"Name\":\"WisdomTree Corn\",\"Type\":\"ETF\",\"Country\":\"UK\",\"Currency\":\"USD\",\"ISIN\":\"JE00BN7KB441\",\"isPrimary\":false,\"previousClose\":18.755,\"previousCloseDate\":\"2026-04-13\"}]");
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(forAsset, domAsset), forAsset, domAsset), date));

            await bus.Command(new RetroactiveCommand<CreateAccount>(new CreateAccount("Main", AccountType.Trading), date));
            await bus.Command(new RetroactiveCommand<DepositAsset>(new  DepositAsset("Main", new Quantity(100, forAsset)), date));

            var assetPairInfo = await bus.QueryAsync(new AssetPairInfoQuery(AssetPair.Fordom(forAsset, domAsset)));
            var ticker = assetPairInfo.Ticker;
            
            await connector.SetAsync(eqQuoteApi.GetUrl(ticker, date),
                "[{\"date\":\"2023-12-01\",\"open\":1.106,\"high\":1.115,\"low\":1.106,\"close\":1.115,\"adjusted_close\":24.53,\"volume\":3255}]");
           
            var res = await bus.QueryAsync(new AccountStatsQuery("Main", domAsset) { Timestamp = date, QueryNet = true });
            Assert.Equal(100, res.Positions.SingleOrDefault()?.Amount);
            Assert.Equal(100*1.115, res.Balance.Amount);
            
            var splitDate = new LocalDateTime(2023, 12, 4, 12, 30).InUtc().ToInstant().ToTime();
            var splitRatio = 0.045;
            await bus.Command(new RetroactiveCommand<AddStockSplit>(new AddStockSplit( AssetPair.Fordom(forAsset, domAsset),splitRatio), splitDate));

            await connector.SetAsync(eqQuoteApi.GetUrl(ticker, splitDate),
                "[{\"date\":\"2023-12-04\",\"open\":24.45,\"high\":24.97,\"low\":24.45,\"close\":24.7325,\"adjusted_close\":24.7325,\"volume\":96}]");
            
            res = await bus.QueryAsync(new AccountStatsQuery("Main", domAsset) { Timestamp = splitDate, QueryNet = true });
            Assert.Equal(100*0.045, res.Positions.SingleOrDefault()?.Amount);
            Assert.Equal(100*24.7325*0.045, res.Balance.Amount, 1e-6);
        }
        
        [Fact]
        public async Task CanGetDerivedAssetPairQuote()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var log = container.GetInstance<ILog>();
            var connector = container.GetInstance<IJSonConnector>();
            var clock = container.GetInstance<IClock>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();
            
            var date = clock.GetCurrentInstant();
            
            var forAsset = new Asset("USD", AssetType.Currency);
            var domAsset = new Asset("GBP", AssetType.Currency);
            var quoteAsset = new Currency("GBX");
            var iukdAsset = new Asset("IUKD", AssetType.Equity);
            
            var fxQuoteApi = webApiProvider.GetQuoteApi(forAsset.AssetType, domAsset.AssetType, true);
            var eqQuoteApi = webApiProvider.GetQuoteApi(iukdAsset.AssetType, quoteAsset.AssetType, true);
            
            await connector.SetAsync(webSearchApi.GetUrl("USDGBP"),
                "[{\"Code\":\"USDGBP\",\"Exchange\":\"FOREX\",\"Name\":\"US Dollar\\/UK Pound Sterling FX Cross Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"GBP\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":0.7546,\"previousCloseDate\":\"2026-03-31\"}]");
            await connector.SetAsync(webSearchApi.GetUrl("IUKD"),
                "[{\"Code\":\"IUKD\",\"Exchange\":\"LSE\",\"Name\":\"iShares UK Dividend UCITS\",\"Type\":\"ETF\",\"Country\":\"UK\",\"Currency\":\"GBX\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":944,\"previousCloseDate\":\"2026-03-25\"},{\"Code\":\"IUKD\",\"Exchange\":\"SW\",\"Name\":\"iShares UK Dividend UCITS ETF GBP (Dist) CHF\",\"Type\":\"ETF\",\"Country\":\"Switzerland\",\"Currency\":\"CHF\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":9.948,\"previousCloseDate\":\"2026-03-25\"}]");
            
            await bus.Command(new RegisterAssetPair(AssetPair.Fordom(domAsset, quoteAsset), domAsset, quoteAsset));
            await bus.Command(new RegisterAssetPair(AssetPair.Fordom(forAsset, domAsset), forAsset, domAsset));
            await bus.Command(new RegisterAssetPair(AssetPair.Fordom(iukdAsset, quoteAsset), iukdAsset, quoteAsset));

            var assetPairInfo = await bus.QueryAsync(new AssetPairInfoQuery(AssetPair.Fordom(forAsset, domAsset)));
            var ticker = assetPairInfo.Ticker;
            
            await connector.SetAsync(fxQuoteApi.GetUrl(ticker, date),
                "{\"code\":\"USDGBP.FOREX\",\"timestamp\":1774967040,\"gmtoffset\":0,\"open\":0.7584,\"high\":0.7598,\"low\":0.754,\"close\":0.7546,\"volume\":0,\"previousClose\":0.7583,\"change\":-0.0037,\"change_p\":-0.4879}");

            assetPairInfo = await bus.QueryAsync(new AssetPairInfoQuery(AssetPair.Fordom(iukdAsset, quoteAsset)));
            ticker = assetPairInfo.Ticker;
            
            await connector.SetAsync(eqQuoteApi.GetUrl(ticker, date),
                "{\"code\":\"IUKD.LSE\",\"timestamp\":1774967100,\"gmtoffset\":0,\"open\":945.1,\"high\":956,\"low\":945.1,\"close\":951.535,\"volume\":789729,\"previousClose\":946.6,\"change\":4.935,\"change_p\":0.5213}");
            
            await bus.Command(new CreateAccount("Main", AccountType.Trading));
            await bus.Command(new DepositAsset("Main", new Quantity(1, iukdAsset)));
           
            await bus.Command(new UpdateQuote(AssetPair.Fordom(iukdAsset, quoteAsset)) { EnforceCache = true });
            await bus.Command(new UpdateQuote(AssetPair.Fordom(forAsset, domAsset)) { EnforceCache = true });

            var res = await bus.QueryAsync(new AssetQuoteQuery(iukdAsset, forAsset));
            Assert.Equal(12.609793267956533, res.Quantity.Amount);
            log.Info($"{iukdAsset} is {res.Quantity} for {res.Timestamp}");
        }
        
        [Fact]
        public async Task CanGetAssetPairRateFromUrl()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();

            var date = new LocalDateTime(2020, 12, 1, 12, 30).InUtc().ToInstant().ToTime();

            var forAsset = new Currency("GBP");
            var domAsset = new Currency("USD");
            var fxQuoteApi = webApiProvider.GetQuoteApi(forAsset.AssetType, domAsset.AssetType, false);
            
            await connector.SetAsync(webSearchApi.GetUrl("GBPUSD"),
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3335,\"previousCloseDate\":\"2026-03-26\"}]");
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair("GBPUSD", forAsset, domAsset), date));
            await bus.Command(new RetroactiveCommand<AddQuoteTicker>(new AddQuoteTicker("GBPUSD", "GBPUSD.FOREX"), date));
            
            var assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(forAsset, domAsset)), date));
            var ticker = assetPairInfo.Ticker;
            
            await connector.SetAsync(fxQuoteApi.GetUrl(ticker, date),
                "[{\"date\":\"2020-12-01\",\"open\":1.3337,\"high\":1.3439,\"low\":1.3319,\"close\":1.3337,\"adjusted_close\":1.3337,\"volume\":908}]");
            
            await bus.Command(new RetroactiveCommand<AddQuoteUrl>(new AddQuoteUrl("GBPUSD", fxQuoteApi.GetUrl(ticker, date)), date));
            
            await bus.Command(new RetroactiveCommand<UpdateQuote<EodhdEodQuoteApiBase.JsonResult, WebSearchApi.JsonResult>>(new UpdateQuote<EodhdEodQuoteApiBase.JsonResult, WebSearchApi.JsonResult>("GBPUSD"), date));
            await bus.Equal(new HistoricalQuery<SingleAssetQuoteQuery, SingleAssetQuote>(new SingleAssetQuoteQuery("GBPUSD"), date), s => s.Price, 1.3337);
        }

        [Fact]
        public async Task CanGetExchangeData()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var queue = container.GetInstance<IMessageQueue>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();

            var webSearchApi = webApiProvider.GetSearchApi();
            var ticker = "IUKD";
            var command =
                new RequestJson<WebSearchApi.JsonResult>(nameof(CanGetExchangeData), webSearchApi.GetUrl(ticker));

            await connector.SetAsync(webSearchApi.GetUrl(ticker),
                "[{\"Code\":\"IUKD\",\"Exchange\":\"LSE\",\"Name\":\"iShares UK Dividend UCITS\",\"Type\":\"ETF\",\"Country\":\"UK\",\"Currency\":\"GBX\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":944,\"previousCloseDate\":\"2026-03-25\"},{\"Code\":\"IUKD\",\"Exchange\":\"SW\",\"Name\":\"iShares UK Dividend UCITS ETF GBP (Dist) CHF\",\"Type\":\"ETF\",\"Country\":\"Switzerland\",\"Currency\":\"CHF\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":9.948,\"previousCloseDate\":\"2026-03-25\"}]");
            
            var obs = queue.Alerts.OfType<JsonRequestCompleted<WebSearchApi.JsonResult>>().FirstAsync()
                .Replay();
            obs.Connect();
            
            await await bus.CommandAsync(command);
            var res = await obs.Timeout(Configuration.Timeout);
            var exchanges = webSearchApi.GetExchanges(res?.Data);
            Assert.Contains(exchanges, x => x == "LSE");
        }
        
        [Fact]
        public async Task CanGetEquityQuote()
        {
            var container = CreateContainer();
            
            var bus = container.GetInstance<IBus>();
            var log = container.GetInstance<ILog>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();

            var domAsset = new Currency("GBX");
            var forAsset = new Asset("IUKD", AssetType.Equity);
            var eqQuoteApi = webApiProvider.GetQuoteApi(forAsset.AssetType, domAsset.AssetType, false);
            
            var date = new LocalDateTime(2026, 3, 17, 12, 30).InUtc().ToInstant().ToTime();
          
            await connector.SetAsync(webSearchApi.GetUrl("IUKD"),
                "[{\"Code\":\"IUKD\",\"Exchange\":\"LSE\",\"Name\":\"iShares UK Dividend UCITS\",\"Type\":\"ETF\",\"Country\":\"UK\",\"Currency\":\"GBX\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":932.3,\"previousCloseDate\":\"2026-03-27\"},{\"Code\":\"IUKD\",\"Exchange\":\"SW\",\"Name\":\"iShares UK Dividend UCITS ETF GBP (Dist) CHF\",\"Type\":\"ETF\",\"Country\":\"Switzerland\",\"Currency\":\"CHF\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":9.988,\"previousCloseDate\":\"2026-03-27\"}]");
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(forAsset, domAsset), forAsset,
                domAsset), Time.MinValue));
            await bus.Command(new RetroactiveCommand<AddQuoteTicker>(new AddQuoteTicker(AssetPair.Fordom(forAsset, domAsset), "IUKD.LSE"), date));
           
            var assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(forAsset, domAsset)), date));
            var ticker = assetPairInfo.Ticker;
            
            await connector.SetAsync(eqQuoteApi.GetUrl(ticker, date),
                "[{\"date\":\"2026-03-17\",\"open\":979.5,\"high\":988.8,\"low\":975.2,\"close\":985,\"adjusted_close\":980.9421,\"volume\":419265}]");
            
            await bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(forAsset, domAsset)), date));
            
            var res = await bus.QueryAsync(new HistoricalQuery<AssetQuoteQuery, AssetQuote>(new AssetQuoteQuery(forAsset, domAsset), date));
            log.Info($"{AssetPair.Fordom(forAsset, domAsset)} is {res.Quantity} for {res.Timestamp}");
            Assert.Equal(985, res.Quantity.Amount);
        }
        
        [Fact]
        private async Task CanGetLatestPairQuote()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var log = container.GetInstance<ILog>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();

            var forAsset = new Currency("GBP");
            var domAsset = new Currency("USD");
            var fxQuoteApi = webApiProvider.GetQuoteApi(forAsset.AssetType, domAsset.AssetType, true);
            
            await connector.SetAsync(webSearchApi.GetUrl("GBPUSD"),
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3335,\"previousCloseDate\":\"2026-03-26\"}]");
           
            await bus.Command(new RegisterAssetPair("GBPUSD", forAsset, domAsset));
            
            var assetPairInfo = await bus.QueryAsync(new AssetPairInfoQuery(AssetPair.Fordom(forAsset, domAsset)));
            var ticker = assetPairInfo.Ticker;
            Assert.Equal("GBPUSD.FOREX", ticker);

            await connector.SetAsync(fxQuoteApi.GetUrl(ticker),
                "{\"code\":\"GBPUSD.FOREX\",\"timestamp\":1774968180,\"gmtoffset\":0,\"open\":1.3185,\"high\":1.3262,\"low\":1.3161,\"close\":1.3235,\"volume\":0,\"previousClose\":1.3172,\"change\":0.0063,\"change_p\":0.4783}");
            
            await bus.Command(new UpdateQuote(AssetPair.Fordom(forAsset, domAsset)) {EnforceCache = true});
            await bus.Command(new UpdateQuote(AssetPair.Fordom(forAsset, domAsset)) {EnforceCache = true});
            
            await bus.IsTrue(new AssetQuoteQuery(forAsset, domAsset), q => q.Quantity.Amount > 1);
            var res = await bus.QueryAsync(new AssetQuoteQuery(forAsset, domAsset));
            log.Info($"{AssetPair.Fordom(forAsset, domAsset)} is {res.Quantity} for {res.Timestamp}");
        }

        [Fact]
        public async Task CanRollbackQuote()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var retroactive = container.GetInstance<IRetroactive>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();
            
            var forAsset = new Currency("GBP");
            var domAsset = new Currency("USD");
            var fxQuoteApi = webApiProvider.GetQuoteApi(forAsset.AssetType, domAsset.AssetType, true);

            await connector.SetAsync(webSearchApi.GetUrl("GBPUSD"),
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3335,\"previousCloseDate\":\"2026-03-26\"}]");
            
            var command = new UpdateQuote(AssetPair.Fordom(forAsset, domAsset)) { EnforceCache = true };
            
            await bus.Command(new RegisterAssetPair("GBPUSD", forAsset, domAsset));
            
            var assetPairInfo = await bus.QueryAsync(new AssetPairInfoQuery(AssetPair.Fordom(forAsset, domAsset)));
            var ticker = assetPairInfo.Ticker;
            Assert.Equal("GBPUSD.FOREX", ticker);
            
            await connector.SetAsync(fxQuoteApi.GetUrl(ticker),
                "{\"code\":\"GBPUSD.FOREX\",\"timestamp\":1774639320,\"gmtoffset\":0,\"open\":1.3334,\"high\":1.3347,\"low\":1.3261,\"close\":1.3265,\"volume\":0,\"previousClose\":1.3335,\"change\":-0.007,\"change_p\":-0.5249}");
            
            await bus.Command(command);

            await retroactive.RollbackCommands(new[] { command });
        }

        [Fact]
        public async Task CanGetHistoricalPairQuote()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();
            
            var date = new LocalDateTime(2020, 12, 1, 12, 30).InUtc().ToInstant().ToTime();
            
            var gbp = new Currency("GBP");
            var usd = new Currency("USD");
            var btc = new Asset("Bitcoin", AssetType.Coin);
            
            var fxQuoteApi = webApiProvider.GetQuoteApi(AssetType.Currency, AssetType.Currency, false);
            var coinQuoteApi = webApiProvider.GetQuoteApi(AssetType.Coin, AssetType.Currency, false);
            
            await connector.SetAsync(webSearchApi.GetUrl("GBPUSD"),
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3335,\"previousCloseDate\":\"2026-03-26\"}]");
            await connector.SetAsync(webSearchApi.GetUrl("Bitcoin-USD"), 
                "[{\"Code\":\"BTC-USD\",\"Exchange\":\"CC\",\"Name\":\"Bitcoin\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":68686.0390625,\"previousCloseDate\":\"2026-04-07\"}]");
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(btc, usd), btc, usd), date));
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(gbp, usd), gbp, usd), date));

            var midDate = new LocalDateTime(2020, 12, 10, 12, 30).InUtc().ToInstant().ToTime();
            var lastDate = new LocalDateTime(2020, 12, 15, 12, 30).InUtc().ToInstant().ToTime(); 
            
            await bus.Command(new RetroactiveCommand<AddQuoteTicker>(new AddQuoteTicker(AssetPair.Fordom(gbp, usd), "GBPUSD.FOREX"), date));
            
            var assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(gbp, usd)), date));
            var ticker = assetPairInfo.Ticker;
            
            await bus.Command(new RetroactiveCommand<AddQuoteTicker>(new AddQuoteTicker(AssetPair.Fordom(btc, usd), "BTC-USD.CC"), date));
            var btcAssetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(btc, usd)), date));
            var btcTicker = btcAssetPairInfo.Ticker;
            
            await connector.SetAsync(fxQuoteApi.GetUrl(ticker, midDate),
                "[{\"date\":\"2020-12-10\",\"open\":1.3367,\"high\":1.339,\"low\":1.325,\"close\":1.3367,\"adjusted_close\":1.3367,\"volume\":1063}]");
            await connector.SetAsync(fxQuoteApi.GetUrl(ticker, lastDate),
                "[{\"date\":\"2020-12-15\",\"open\":1.3325,\"high\":1.3449,\"low\":1.3285,\"close\":1.3331,\"adjusted_close\":1.3331,\"volume\":1234}]");
            
            await bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(gbp, usd)), midDate));
            await bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(gbp, usd)), lastDate));
            
            await connector.SetAsync(coinQuoteApi.GetUrl(btcTicker, midDate),
                "[{\"date\":\"2020-12-10\",\"open\":18553.29972814,\"high\":18553.29972814,\"low\":17957.06521319,\"close\":18264.99210672,\"adjusted_close\":18264.99210672,\"volume\":25547132265}]");
            
            await bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(btc, usd)), midDate));

            await bus.Equal(new AssetQuoteQuery(btc, gbp) { Timestamp = midDate }, a => a.Timestamp, midDate.ToInstant());
            var quote = await bus.QueryAsync(new AssetQuoteQuery(btc, gbp) { Timestamp = midDate });
            var historicalQuote = await bus.QueryAsync(new HistoricalQuery<AssetQuoteQuery, AssetQuote>(new AssetQuoteQuery(btc, gbp), midDate));
            Assert.Equal(quote.Quantity, historicalQuote.Quantity);
            Assert.Equal(quote.Timestamp, historicalQuote.Timestamp);
        }   
        
        [Fact]
        public async Task CanGetAssetPairRateFromUrlGeneric()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            var webApiProvider = container.GetInstance<IWebApiProvider>();
            var webSearchApi = webApiProvider.GetSearchApi();

            var date = new LocalDateTime(2020, 12, 1, 12, 30).InUtc().ToInstant().ToTime();

            var gbp = new Currency("GBP");
            var usd = new Currency("USD");
            var btc = new Asset("Bitcoin",  AssetType.Coin);
         
            var fxQuoteApi = webApiProvider.GetQuoteApi(AssetType.Currency, AssetType.Currency, false);
            var coinQuoteApi = webApiProvider.GetQuoteApi(AssetType.Coin, AssetType.Currency, false);
            
            await connector.SetAsync(webSearchApi.GetUrl("GBPUSD"),
                "[{\"Code\":\"GBPUSD\",\"Exchange\":\"FOREX\",\"Name\":\"UK Pound Sterling\\/US Dollar FX Spot Rate\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":1.3335,\"previousCloseDate\":\"2026-03-26\"}]");

            await connector.SetAsync(webSearchApi.GetUrl("Bitcoin-USD"), 
                "[{\"Code\":\"BTC-USD\",\"Exchange\":\"CC\",\"Name\":\"Bitcoin\",\"Type\":\"Currency\",\"Country\":\"Unknown\",\"Currency\":\"USD\",\"ISIN\":null,\"isPrimary\":false,\"previousClose\":68686.0390625,\"previousCloseDate\":\"2026-04-07\"}]");
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(btc, usd), btc, usd), date));
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(gbp, usd), gbp, usd), date));
 
            await bus.Command(new RetroactiveCommand<AddQuoteTicker>(new AddQuoteTicker(AssetPair.Fordom(gbp, usd), "GBPUSD.FOREX"), date));
            
            var assetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(gbp, usd)), date));
            var ticker = assetPairInfo.Ticker;

            await bus.Command(new RetroactiveCommand<AddQuoteTicker>(new AddQuoteTicker(AssetPair.Fordom(btc, usd), "BTC-USD.CC"), date));
            var btcAssetPairInfo = await bus.QueryAsync(new HistoricalQuery<AssetPairInfoQuery, AssetPairInfo>(new AssetPairInfoQuery(AssetPair.Fordom(btc, usd)), date));
            var btcTicker = btcAssetPairInfo.Ticker;
            
            await connector.SetAsync(fxQuoteApi.GetUrl(ticker, date),
                "[{\"date\":\"2020-12-01\",\"open\":1.3337,\"high\":1.3439,\"low\":1.3319,\"close\":1.3337,\"adjusted_close\":1.3337,\"volume\":908}]");
            await connector.SetAsync(coinQuoteApi.GetUrl(btcTicker,date),
                "[{\"date\":\"2020-12-01\",\"open\":19633.77044727,\"high\":19845.97548328,\"low\":18321.92093045,\"close\":18802.99829969,\"adjusted_close\":18802.99829969,\"volume\":49633658712}]");

            await bus.Command(new RetroactiveCommand<AddQuoteUrl>(new AddQuoteUrl(AssetPair.Fordom(btc, usd), coinQuoteApi.GetUrl(btcTicker, date)), date));
            await bus.Command(new RetroactiveCommand<AddQuoteUrl>(new AddQuoteUrl(AssetPair.Fordom(gbp, usd), fxQuoteApi.GetUrl(ticker, date)), date));
            
            await bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(gbp, usd)), date));
            await bus.Equal(new HistoricalQuery<SingleAssetQuoteQuery, SingleAssetQuote>(new SingleAssetQuoteQuery(AssetPair.Fordom(gbp, usd)), date), s => s.Price, 1.3337);
            await bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(btc, usd)), date));
            await bus.Equal(new HistoricalQuery<AssetQuoteQuery, AssetQuote>(new AssetQuoteQuery(btc, gbp), date), s => Math.Round(s.Quantity.Amount, 6), Math.Round(18802.99829969 / 1.3337, 6));
        }
    }
}