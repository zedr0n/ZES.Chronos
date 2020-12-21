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
        public async void CanUseCurrencyPair()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var timeline = container.GetInstance<ITimeline>();

            var forAsset = new Currency("GBP");
            var domAsset = new Currency("USD");

            await bus.Command(new RegisterAssetPair("GBPUSD", forAsset, domAsset));
            await bus.Command(new AddQuote("GBPUSD", timeline.Now, 1.2));
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
            
            await bus.Command(new UpdateQuote<Api.Fx.JsonResult>("GBPUSD"));
            await bus.Equal(new SingleAssetPriceQuery("GBPUSD"), s => s.Price, 1.3287850671);
            
            await bus.Command(new RetroactiveCommand<UpdateQuote<Api.Fx.JsonResult>>(new UpdateQuote<Api.Fx.JsonResult>("GBPUSD"), date));
            await bus.Equal(new HistoricalQuery<SingleAssetPriceQuery, SingleAssetPrice>(new SingleAssetPriceQuery("GBPUSD"), date), s => s.Price, 1.332769104);
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
            
            await bus.Command(new UpdateQuote(AssetPair.Fordom(gbp, usd)));
            await bus.Equal(new SingleAssetPriceQuery(AssetPair.Fordom(gbp, usd)), s => s.Price, 1.3287850671);

            await bus.Command(new UpdateQuote(AssetPair.Fordom(btc, usd)));
            await bus.Equal(new SingleAssetPriceQuery(AssetPair.Fordom(btc, usd)), s => s.Price, 23518.31842054723);
            
            await bus.Command(new RetroactiveCommand<UpdateQuote>(new UpdateQuote(AssetPair.Fordom(gbp, usd)), date));
            await bus.Equal(new HistoricalQuery<SingleAssetPriceQuery, SingleAssetPrice>(new SingleAssetPriceQuery(AssetPair.Fordom(gbp, usd)), date), s => s.Price, 1.332769104);
        }
    }
}