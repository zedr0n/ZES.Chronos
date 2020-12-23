using System;
using Chronos.Accounts.Queries;
using Chronos.Core;
using Chronos.Core.Commands;
using Chronos.Core.Queries;
using NodaTime;
using NodaTime.Calendars;
using Xunit;
using Xunit.Abstractions;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Pipes;
using ZES.Tests;

namespace Chronos.Tests
{
    public class AssetTests : ChronosTest
    {
        public AssetTests(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
        }

        [Fact]
        public async void CanRecordTransaction()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            
            var gbp = new Currency("GBP");
            var usd = new Currency("USD");

            await bus.Command(new RegisterAssetPair("GBPUSD", gbp, usd));
            await bus.Command(new AddQuote("GBPUSD", timeline.Now, 1.2));

            await bus.Command(new RecordTransaction("Tx", new Quantity(100, gbp), Transaction.TransactionType.Spend, string.Empty));
            await bus.Equal(new TransactionInfoQuery("Tx", usd), t => t.Quantity.Amount, 100 * 1.2);
        }

        [Fact]
        public async void CanUseTransactionQuote()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();
            
            var gbp = new Currency("GBP");
            var usd = new Currency("USD");

            await bus.Command(new RegisterAssetPair("GBPUSD", gbp, usd));
            await bus.Command(new AddQuote("GBPUSD", timeline.Now, 1.2));

            await bus.Command(new RecordTransaction("Tx", new Quantity(100, gbp), Transaction.TransactionType.Spend, string.Empty));
            await bus.Command(new AddTransactionQuote("Tx", new Quantity(110, usd)));
            await bus.Equal(new TransactionInfoQuery("Tx", usd), t => t.Quantity.Amount, 110);
        }

        [Fact]
        public async void CanUseCurrencyPair()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();

            var forAsset = new Currency("GBP");
            var domAsset = new Currency("USD");

            await bus.Command(new RegisterAssetPair("GBPUSD", forAsset, domAsset));
            await bus.Command(new AddQuote("GBPUSD", timeline.Now, 1.2));

            var assetsInfo = await bus.QueryAsync(new AssetPairsInfoQuery());
            Assert.Contains(forAsset, assetsInfo.Assets);
            Assert.Contains(domAsset, assetsInfo.Assets);
        }

        [Fact]
        public async void CanGetAssetPairRateFromUrl()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            
            var date = new LocalDateTime(2020, 12, 1, 12, 30).InUtc().ToInstant();

            var forAsset = new Currency("GBP");
            var domAsset = new Currency("USD");
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair("GBPUSD", forAsset, domAsset), date));
            await bus.Command(new RetroactiveCommand<AddQuoteUrl>(new AddQuoteUrl("GBPUSD", Api.Fx.Url(forAsset, domAsset)), date));
            
            await bus.Command(new RetroactiveCommand<UpdateQuote<Api.Fx.JsonResult>>(new UpdateQuote<Api.Fx.JsonResult>("GBPUSD"), date));
            await bus.Equal(new HistoricalQuery<SingleAssetPriceQuery, SingleAssetPrice>(new SingleAssetPriceQuery("GBPUSD"), date), s => s.Price, 1.332769104);
        }

        [Fact]
        public async void CanGetLatestPairQuote()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var log = container.GetInstance<ILog>();
            
            var forAsset = new Currency("GBP");
            var domAsset = new Currency("USD");
            
            await bus.Command(new RegisterAssetPair("GBPUSD", forAsset, domAsset));
            await bus.Command(new UpdateQuote(AssetPair.Fordom(forAsset, domAsset)));
            await bus.IsTrue(new AssetPriceQuery(forAsset, domAsset), q => q.Price > 1);
            var res = await bus.QueryAsync(new AssetPriceQuery(forAsset, domAsset));
            log.Info($"{AssetPair.Fordom(forAsset, domAsset)} is {res.Price} for {res.Timestamp}");
        }
        
        [Fact]
        public async void CanGetAssetPairRateFromUrlGeneric()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            
            var date = new LocalDateTime(2020, 12, 1, 12, 30).InUtc().ToInstant();

            var gbp = new Currency("GBP");
            var usd = new Currency("USD");
            var btc = new Asset("Bitcoin", "BTC", Asset.Type.Coin);

            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(btc, usd), btc, usd), date));
            await bus.Command(new RetroactiveCommand<AddQuoteUrl>(new AddQuoteUrl(AssetPair.Fordom(btc, usd), Api.Coin.Url(btc, usd)), date));
            
            await bus.Command(new RetroactiveCommand<RegisterAssetPair>(new RegisterAssetPair(AssetPair.Fordom(gbp, usd), gbp, usd), date));
            await bus.Command(new RetroactiveCommand<AddQuoteUrl>(new AddQuoteUrl(AssetPair.Fordom(gbp, usd), Api.Fx.Url(gbp, usd)), date));
            
            await bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(gbp, usd)), date));
            await bus.Equal(new HistoricalQuery<SingleAssetPriceQuery, SingleAssetPrice>(new SingleAssetPriceQuery(AssetPair.Fordom(gbp, usd)), date), s => s.Price, 1.332769104);
            await bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(btc, usd)), date));
            await bus.Equal(new HistoricalQuery<AssetPriceQuery, AssetPrice>(new AssetPriceQuery(btc, gbp), date), s => Math.Round(s.Price, 6), Math.Round(19609.52143957559 / 1.332769104, 6));
        }
    }
}