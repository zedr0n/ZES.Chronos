using System;
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
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Infrastructure;
using ZES.Interfaces.Net;
using IClock = ZES.Interfaces.Clocks.IClock;

namespace Chronos.Tests;

public class FutureTests : ChronosTest
{
    public FutureTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
    }

    [Fact]
    public async Task CanPurchaseAssetInTheFutureFlat()
    {
        var container = CreateContainer();
        var bus = container.GetInstance<IBus>();
        var connector = container.GetInstance<IJSonConnector>();
        var webApiProvider = container.GetInstance<IWebApiProvider>();
        var branchManager = container.GetInstance<IBranchManager>();
        var clock = container.GetInstance<IClock>();
        
        var webSearchApi = webApiProvider.GetSearchApi();
        await bus.Command(new CreateAccount("Account", AccountType.Trading));
            
        var asset = new Asset("IUKD", AssetType.Equity);
        var ccy = new Currency("GBP");
        var quoteCcy = new Currency("GBX");
            
        await connector.SetAsync(webSearchApi.GetUrl(asset.AssetId),
            "[{\"Code\":\"IUKD\",\"Exchange\":\"LSE\",\"Name\":\"iShares UK Dividend UCITS\",\"Type\":\"ETF\",\"Country\":\"UK\",\"Currency\":\"GBX\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":944,\"previousCloseDate\":\"2026-03-25\"},{\"Code\":\"IUKD\",\"Exchange\":\"SW\",\"Name\":\"iShares UK Dividend UCITS ETF GBP (Dist) CHF\",\"Type\":\"ETF\",\"Country\":\"Switzerland\",\"Currency\":\"CHF\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":9.948,\"previousCloseDate\":\"2026-03-25\"}]");
        
        await bus.Command(new RegisterAssetPair(ccy, quoteCcy));
        await bus.Command(new RegisterAssetPair(asset, quoteCcy, "UK"));

        var rule = new FutureQuoteRuleFlat();
        await bus.Command(new AddFutureQuoteRule(AssetPair.Fordom(asset, quoteCcy), rule.RuleType, rule.ToDictionary()));
        
        var now = clock.GetCurrentInstant();
        var webQuoteApi = webApiProvider.GetQuoteApi(AssetType.Equity, AssetType.Currency, now.IsWorkingDay());
        
        if (now.IsWorkingDay())
        {
            await connector.SetAsync(webQuoteApi.GetUrl("IUKD.LSE", (Time)null, true),
                "{\"code\":\"IUKD.LSE\",\"timestamp\":1780068900,\"gmtoffset\":0,\"open\":1001.4,\"high\":1002.809,\"low\":994.1,\"close\":994.1,\"volume\":233229,\"previousClose\":999.7,\"change\":-5.6,\"change_p\":-0.5602}");
        }
        else
        {
            await connector.SetAsync(webQuoteApi.GetUrl("IUKD.LSE", now.RollToWorkingDay("UK"), true),
                "[{\"date\":\"2026-06-05\",\"open\":997.2,\"high\":997.2,\"low\":989.5,\"close\":994.1,\"adjusted_close\":990,\"volume\":116118}]");
        }        
        
        var assetQuote = await bus.QueryAsync(new AssetQuoteQuery(asset, ccy) { UpdateQuote = true, EnforceCache = true });
        
        var branch = "Future";
        var timeline = await branchManager.Branch(branch, now);
        timeline.Advance(Period.FromWeeks(1));
        
        Assert.Equal(now.PlusPeriod(Period.FromWeeks(1)), timeline.Now);
        
        await bus.Command(new TransactAsset("Account",new Quantity(100, asset), assetQuote.Quantity * 100 ));
           
        var stats = await bus.QueryAsync(new AccountStatsQuery("Account", ccy) { UpdateQuotes = true });
        Assert.Equal(0.0, stats.Balance.Amount, 4);
        Assert.Single(stats.Positions);
        Assert.Equal(asset, stats.Positions[0].Denominator);
        Assert.Equal(assetQuote.Quantity.Amount * 100,stats.Values[0].Amount);
        Assert.Equal(-assetQuote.Quantity.Amount * 100, stats.CashBalance.Amount, 6);
        Assert.Equal(assetQuote.Quantity.Amount * 100, stats.CostBasis[0].Amount);
    }
    
    [Fact]
    public async Task CanPurchaseAssetInTheFutureFixed()
    {
        var container = CreateContainer();
        var bus = container.GetInstance<IBus>();
        var connector = container.GetInstance<IJSonConnector>();
        var webApiProvider = container.GetInstance<IWebApiProvider>();
        var branchManager = container.GetInstance<IBranchManager>();
        var clock = container.GetInstance<IClock>();
        
        var webSearchApi = webApiProvider.GetSearchApi();

        await bus.Command(new CreateAccount("Account", AccountType.Trading));
            
        var asset = new Asset("IUKD", AssetType.Equity);
        var ccy = new Currency("GBP");
        var quoteCcy = new Currency("GBX");
            
        await connector.SetAsync(webSearchApi.GetUrl(asset.AssetId),
            "[{\"Code\":\"IUKD\",\"Exchange\":\"LSE\",\"Name\":\"iShares UK Dividend UCITS\",\"Type\":\"ETF\",\"Country\":\"UK\",\"Currency\":\"GBX\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":944,\"previousCloseDate\":\"2026-03-25\"},{\"Code\":\"IUKD\",\"Exchange\":\"SW\",\"Name\":\"iShares UK Dividend UCITS ETF GBP (Dist) CHF\",\"Type\":\"ETF\",\"Country\":\"Switzerland\",\"Currency\":\"CHF\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":9.948,\"previousCloseDate\":\"2026-03-25\"}]");

        await bus.Command(new RegisterAssetPair(ccy, quoteCcy));
        await bus.Command(new RegisterAssetPair(asset, quoteCcy, "UK"));

        var rule = new FutureQuoteRuleFixed(1000.0);
        await bus.Command(new AddFutureQuoteRule(AssetPair.Fordom(asset, quoteCcy), rule.RuleType, rule.ToDictionary()));
        
        var now = clock.GetCurrentInstant();
        var webQuoteApi = webApiProvider.GetQuoteApi(AssetType.Equity, AssetType.Currency, now.IsWorkingDay());
        
        if (now.IsWorkingDay())
        {
            await connector.SetAsync(webQuoteApi.GetUrl("IUKD.LSE", (Time)null, true),
                "{\"code\":\"IUKD.LSE\",\"timestamp\":1780068900,\"gmtoffset\":0,\"open\":1001.4,\"high\":1002.809,\"low\":994.1,\"close\":994.1,\"volume\":233229,\"previousClose\":999.7,\"change\":-5.6,\"change_p\":-0.5602}");
        }
        else
        {
            await connector.SetAsync(webQuoteApi.GetUrl("IUKD.LSE", now.RollToWorkingDay("UK"), true),
                "[{\"date\":\"2026-06-05\",\"open\":997.2,\"high\":997.2,\"low\":989.5,\"close\":994.1,\"adjusted_close\":990,\"volume\":116118}]");
        }        
        
        var assetQuote = await bus.QueryAsync(new AssetQuoteQuery(asset, ccy) { UpdateQuote = true, EnforceCache = true });
        
        var branch = "Future";
        var timeline = await branchManager.Branch(branch, now);
        timeline.Advance(Period.FromWeeks(1));
        
        Assert.Equal(now.PlusPeriod(Period.FromWeeks(1)), timeline.Now);
        
        await bus.Command(new TransactAsset("Account",new Quantity(100, asset), assetQuote.Quantity * 100 ));
           
        var stats = await bus.QueryAsync(new AccountStatsQuery("Account", ccy) { UpdateQuotes = true });
        Assert.Equal(100*(10.0 - assetQuote.Quantity.Amount), stats.Balance.Amount, 4);
        Assert.Single(stats.Positions);
        Assert.Equal(asset, stats.Positions[0].Denominator);
        Assert.Equal(100*10.0,stats.Values[0].Amount);
        Assert.Equal(-assetQuote.Quantity.Amount * 100, stats.CashBalance.Amount, 6);
        Assert.Equal(assetQuote.Quantity.Amount * 100, stats.CostBasis[0].Amount);
    }

    [Fact]
    public async Task CanPurchaseAssetInTheFutureConstantGrowth()
    {
        var container = CreateContainer();
        var bus = container.GetInstance<IBus>();
        var connector = container.GetInstance<IJSonConnector>();
        var webApiProvider = container.GetInstance<IWebApiProvider>();
        var branchManager = container.GetInstance<IBranchManager>();
        var clock = container.GetInstance<IClock>();

        var webSearchApi = webApiProvider.GetSearchApi();

        await bus.Command(new CreateAccount("Account", AccountType.Trading).ToRetroactiveCommand(Time.MinValue));

        var asset = new Asset("IUKD", AssetType.Equity);
        var ccy = new Currency("GBP");
        var quoteCcy = new Currency("GBX");

        await connector.SetAsync(webSearchApi.GetUrl(asset.AssetId),
            "[{\"Code\":\"IUKD\",\"Exchange\":\"LSE\",\"Name\":\"iShares UK Dividend UCITS\",\"Type\":\"ETF\",\"Country\":\"UK\",\"Currency\":\"GBX\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":944,\"previousCloseDate\":\"2026-03-25\"},{\"Code\":\"IUKD\",\"Exchange\":\"SW\",\"Name\":\"iShares UK Dividend UCITS ETF GBP (Dist) CHF\",\"Type\":\"ETF\",\"Country\":\"Switzerland\",\"Currency\":\"CHF\",\"ISIN\":\"IE00B0M63060\",\"isPrimary\":false,\"previousClose\":9.948,\"previousCloseDate\":\"2026-03-25\"}]");

        await bus.Command(new RegisterAssetPair(ccy, quoteCcy).ToRetroactiveCommand(Time.MinValue));
        await bus.Command(new RegisterAssetPair(asset, quoteCcy, "UK").ToRetroactiveCommand(Time.MinValue));

        var rate = 0.06;
        var rule = new FutureQuoteRuleConstantGrowth(rate);
        await bus.Command(new AddFutureQuoteRule(AssetPair.Fordom(asset, quoteCcy), rule.RuleType, rule.ToDictionary()).ToRetroactiveCommand(Time.MinValue));
        
        var now = clock.GetCurrentInstant();
        var webQuoteApi = webApiProvider.GetQuoteApi(AssetType.Equity, AssetType.Currency, now.IsWorkingDay());
        
        if (now.IsWorkingDay())
        {
            await connector.SetAsync(webQuoteApi.GetUrl("IUKD.LSE", (Time)null, true),
                "{\"code\":\"IUKD.LSE\",\"timestamp\":1780068900,\"gmtoffset\":0,\"open\":1001.4,\"high\":1002.809,\"low\":994.1,\"close\":994.1,\"volume\":233229,\"previousClose\":999.7,\"change\":-5.6,\"change_p\":-0.5602}");
        }
        else
        {
            await connector.SetAsync(webQuoteApi.GetUrl("IUKD.LSE", now.RollToWorkingDay("UK"), true),
                "[{\"date\":\"2026-06-05\",\"open\":997.2,\"high\":997.2,\"low\":989.5,\"close\":994.1,\"adjusted_close\":990,\"volume\":116118}]");
        }            
        
        var assetQuote = await bus.QueryAsync(new AssetQuoteQuery(asset, ccy) { UpdateQuote = true, EnforceCache = true });

        var branch = "Future";
        var timeline = await branchManager.Branch(branch, now);
        timeline.Advance(Period.FromYears(1));

        Assert.Equal(now.PlusPeriod(Period.FromYears(1)), timeline.Now);

        await bus.Command(new TransactAsset("Account", new Quantity(100, asset), assetQuote.Quantity * 100));

        var stats = await bus.QueryAsync(new AccountStatsQuery("Account", ccy) { UpdateQuotes = true });
        var futureQuote = assetQuote.Quantity.Amount * Math.Pow(1 + rate, (timeline.Now - now.RollToWorkingDay("UK").CloseOfDay()).TotalDays / 365);
        Assert.Equal(100 * (futureQuote - assetQuote.Quantity.Amount), stats.Balance.Amount, 6);
        Assert.Single(stats.Positions);
        Assert.Equal(asset, stats.Positions[0].Denominator);
        Assert.Equal(100 * futureQuote, stats.Values[0].Amount, 6);
        Assert.Equal(-assetQuote.Quantity.Amount * 100, stats.CashBalance.Amount, 6);
        Assert.Equal(assetQuote.Quantity.Amount * 100, stats.CostBasis[0].Amount);
    }
}