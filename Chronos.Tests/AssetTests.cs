using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chronos.Core;
using Chronos.Core.Commands;
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
            
            var gbp = new Currency("GBP");
            var usd = new Currency("USD");

            await bus.Command(new RegisterAssetPair("GBPUSD", gbp, usd));
            await bus.Command(new AddQuote("GBPUSD", timeline.Now.ToInstant(), 1.2));

            await bus.Command(new RecordTransaction("Tx", new Quantity(100, gbp), Transaction.TransactionType.Spend, string.Empty));
            await bus.Equal(new TransactionInfoQuery("Tx", usd), t => t.Quantity.Amount, 100 * 1.2);
        }

        [Fact]
        public async Task CanUseTransactionQuote()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            
            var gbp = new Currency("GBP");
            var usd = new Currency("USD");

            await bus.Command(new RegisterAssetPair("GBPUSD", gbp, usd));
            await bus.Command(new AddQuote("GBPUSD", timeline.Now.ToInstant(), 1.2));

            await bus.Command(new RecordTransaction("Tx", new Quantity(100, gbp), Transaction.TransactionType.Spend, string.Empty));
            await bus.Command(new AddTransactionQuote("Tx", new Quantity(110, usd)));
            await bus.Equal(new TransactionInfoQuery("Tx", usd), t => t.Quantity.Amount, 110);
        }

        [Fact]
        public async Task CanUseCurrencyPair()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();

            var forAsset = new Currency("GBP");
            var domAsset = new Currency("USD");

            await bus.Command(new RegisterAssetPair("GBPUSD", forAsset, domAsset));
            await bus.Command(new AddQuote("GBPUSD", timeline.Now.ToInstant(), 1.2));

            var assetsInfo = await bus.QueryAsync(new AssetPairsInfoQuery());
            Assert.Contains(forAsset, assetsInfo.Assets);
            Assert.Contains(domAsset, assetsInfo.Assets);
        }

        [Fact]
        public async Task CanGetAssetPairRateFromUrl()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();

            var date = new LocalDateTime(2020, 12, 1, 12, 30).InUtc().ToInstant().ToTime();

            var forAsset = new Currency("GBP");
            var domAsset = new Currency("USD");
            
            await connector.SetAsync(Api.Fx.JsonResult.GetUrl(forAsset, domAsset).Replace("$date", date.ToString(Api.Fx.JsonResult.GetDateFormat(), new DateTimeFormatInfo())),
                "[{\"date\":\"2020-12-01\",\"open\":1.3337,\"high\":1.3439,\"low\":1.3319,\"close\":1.3337,\"adjusted_close\":1.3337,\"volume\":908}]");
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair("GBPUSD", forAsset, domAsset), date));
            await bus.Command(new RetroactiveCommand<AddQuoteUrl>(new AddQuoteUrl("GBPUSD", Api.Fx.JsonResult.GetUrl(forAsset, domAsset)), date));
            
            await bus.Command(new RetroactiveCommand<UpdateQuote<Api.Fx.JsonResult>>(new UpdateQuote<Api.Fx.JsonResult>("GBPUSD"), date));
            await bus.Equal(new HistoricalQuery<SingleAssetQuoteQuery, SingleAssetQuote>(new SingleAssetQuoteQuery("GBPUSD"), date), s => s.Price, 1.3337);
        }

        [Fact]
        public async Task CanGetExchangeData()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var queue = container.GetInstance<IMessageQueue>();
            var connector = container.GetInstance<IJSonConnector>();

            var ticker = "IUKD";
            var command =
                new RequestJson<Api.TickerSearch.JsonResult>(nameof(CanGetExchangeData), Api.TickerSearch.JsonResult.GetUrl(ticker));

            await connector.SetAsync(Api.TickerSearch.JsonResult.GetUrl(ticker),
                "[{\"Code\":\"IUKD\",\"Exchange\":\"LSE\",\"Name\":\"iShares UK Dividend UCITS\",\"Type\":\"ETF\",\"Country\":\"UK\",\"Currency\":\"GBX\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":944,\"previousCloseDate\":\"2026-03-25\"},{\"Code\":\"IUKD\",\"Exchange\":\"SW\",\"Name\":\"iShares UK Dividend UCITS ETF GBP (Dist) CHF\",\"Type\":\"ETF\",\"Country\":\"Switzerland\",\"Currency\":\"CHF\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":9.948,\"previousCloseDate\":\"2026-03-25\"}]");
            
            var obs = queue.Alerts.OfType<JsonRequestCompleted<Api.TickerSearch.JsonResult>>().FirstAsync()
                .Replay();
            obs.Connect();
            
            await await bus.CommandAsync(command);
            var res = await obs.Timeout(Configuration.Timeout);
            var exchanges = Api.TickerSearch.JsonResult.GetExchanges(res?.Data);
            Assert.Contains(exchanges, x => x == "LSE");
        }
        
        [Fact]
        public async Task CanGetEquityQuote()
        {
            var container = CreateContainer();
            
            var bus = container.GetInstance<IBus>();
            var log = container.GetInstance<ILog>();
            var connector = container.GetInstance<IJSonConnector>();

            var domAsset = new Currency("GBP");
            var forAsset = new Asset("IUKD", "IUKD.LSE", AssetType.Equity);
           
            var date = new LocalDateTime(2026, 3, 17, 12, 30).InUtc().ToInstant().ToTime();
           
            await connector.SetAsync(Api.Equity.JsonResult.GetUrl(forAsset, domAsset).Replace("$date", date.ToString(Api.Equity.JsonResult.GetDateFormat(), new DateTimeFormatInfo())),
                "[{\"date\":\"2026-03-17\",\"open\":979.5,\"high\":988.8,\"low\":975.2,\"close\":985,\"adjusted_close\":980.9421,\"volume\":419265}]");
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(forAsset, domAsset), forAsset,
                domAsset), Time.MinValue));
            
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
            
            var forAsset = new Currency("GBP");
            var domAsset = new Currency("USD");
            
            await bus.Command(new RegisterAssetPair("GBPUSD", forAsset, domAsset));
            await bus.Command(new UpdateQuote(AssetPair.Fordom(forAsset, domAsset)));
            
            await bus.Command(new UpdateQuote(AssetPair.Fordom(forAsset, domAsset)));
            await log.Errors.Observable.FirstAsync(e => e.Message.Contains("Quote already added"));
            
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
            
            var forAsset = new Currency("GBP");
            var domAsset = new Currency("USD");

            var command = new UpdateQuote(AssetPair.Fordom(forAsset, domAsset));
            
            await bus.Command(new RegisterAssetPair("GBPUSD", forAsset, domAsset));
            await bus.Command(command);

            await retroactive.RollbackCommands(new[] { command });
        }

        [Fact]
        public async Task CanGetHistoricalPairQuote()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var connector = container.GetInstance<IJSonConnector>();
            
            var date = new LocalDateTime(2020, 12, 1, 12, 30).InUtc().ToInstant().ToTime();
            
            var gbp = new Currency("GBP");
            var usd = new Currency("USD");
            var btc = new Asset("Bitcoin", "BTC", AssetType.Coin);
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(btc, usd), btc, usd), date));
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(gbp, usd), gbp, usd), date));

            var midDate = new LocalDateTime(2020, 12, 10, 12, 30).InUtc().ToInstant().ToTime();
            var lastdate = new LocalDateTime(2020, 12, 15, 12, 30).InUtc().ToInstant().ToTime(); 
            
            await connector.SetAsync(Api.Fx.JsonResult.GetUrl(gbp, usd).Replace("$date", midDate.ToString(Api.Fx.JsonResult.GetDateFormat(), new DateTimeFormatInfo())),
                "[{\"date\":\"2020-12-10\",\"open\":1.3367,\"high\":1.339,\"low\":1.325,\"close\":1.3367,\"adjusted_close\":1.3367,\"volume\":1063}]");
            await connector.SetAsync(Api.Fx.JsonResult.GetUrl(gbp, usd).Replace("$date", lastdate.ToString(Api.Fx.JsonResult.GetDateFormat(), new DateTimeFormatInfo())),
                "[{\"date\":\"2020-12-15\",\"open\":1.3325,\"high\":1.3449,\"low\":1.3285,\"close\":1.3331,\"adjusted_close\":1.3331,\"volume\":1234}]");
            
            await bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(gbp, usd)), midDate));
            await bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(gbp, usd)), lastdate));
            
            await connector.SetAsync(Api.Coin.JsonResult.GetUrl(btc, usd).Replace("$date", midDate.ToString(Api.Coin.JsonResult.GetDateFormat(), new DateTimeFormatInfo())),
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

            var date = new LocalDateTime(2020, 12, 1, 12, 30).InUtc().ToInstant().ToTime();

            var gbp = new Currency("GBP");
            var usd = new Currency("USD");
            var btc = new Asset("Bitcoin", "BTC", AssetType.Coin);

            await connector.SetAsync(Api.Fx.JsonResult.GetUrl(gbp, usd).Replace("$date", date.ToString(Api.Fx.JsonResult.GetDateFormat(), new DateTimeFormatInfo())),
                "[{\"date\":\"2020-12-01\",\"open\":1.3337,\"high\":1.3439,\"low\":1.3319,\"close\":1.3337,\"adjusted_close\":1.3337,\"volume\":908}]");
            await connector.SetAsync(Api.Coin.JsonResult.GetUrl(btc, usd).Replace("$date", date.ToString(Api.Coin.JsonResult.GetDateFormat(), new DateTimeFormatInfo())),
                "[{\"date\":\"2020-12-01\",\"open\":19633.77044727,\"high\":19845.97548328,\"low\":18321.92093045,\"close\":18802.99829969,\"adjusted_close\":18802.99829969,\"volume\":49633658712}]");

            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(btc, usd), btc, usd), date));
            await bus.Command(new RetroactiveCommand<AddQuoteUrl>(new AddQuoteUrl(AssetPair.Fordom(btc, usd), Api.Coin.JsonResult.GetUrl(btc, usd)), date));
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(gbp, usd), gbp, usd), date));
            await bus.Command(new RetroactiveCommand<AddQuoteUrl>(new AddQuoteUrl(AssetPair.Fordom(gbp, usd), Api.Fx.JsonResult.GetUrl(gbp, usd)), date));
            
            await bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(gbp, usd)), date));
            await bus.Equal(new HistoricalQuery<SingleAssetQuoteQuery, SingleAssetQuote>(new SingleAssetQuoteQuery(AssetPair.Fordom(gbp, usd)), date), s => s.Price, 1.3337);
            await bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(btc, usd)), date));
            await bus.Equal(new HistoricalQuery<AssetQuoteQuery, AssetQuote>(new AssetQuoteQuery(btc, gbp), date), s => Math.Round(s.Quantity.Amount, 6), Math.Round(18802.99829969 / 1.3337, 6));
        }
    }
}