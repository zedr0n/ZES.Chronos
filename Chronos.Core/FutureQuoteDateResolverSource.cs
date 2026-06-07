using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Clocks;

namespace Chronos.Core;

/// <summary>
/// Provides a source implementation for resolving quote data for future dates using a clock-based approach.
/// </summary>
public class FutureQuoteDateResolverSource(IClock clock, DefaultQuoteDataResolverSource defaultResolver, IFutureQuoteRuleRegistry ruleRegistry) : IQuoteDataResolverSource
{
    /// <inheritdoc/>
    public int Priority { get; } = 1;

    /// <inheritdoc/>
    public bool CanResolve(QuoteDataRequest request)
    {
        var now = request.Now > clock.GetCurrentInstant() ? clock.GetCurrentInstant() : request.Now; 
        var canResolve = request.Timestamps.Any(t => t.Date() > now.Date());
        if (!canResolve)
            return false;
        
        var assetPair = request.AssetPair;
        var rules = assetPair.FutureQuoteRules
            .Select(x => ruleRegistry.Create(x.RuleType, x.Parameters))
            .Where(x => x.AppliesTo(request))
            .ToList();
        
        return rules.Count > 0;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<Time, double>> ResolveValues(QuoteDataRequest request)
    {
        var assetPair = request.AssetPair;
        var rules = assetPair.FutureQuoteRules
            .Select(x => ruleRegistry.Create(x.RuleType, x.Parameters))
            .Where(x => x.AppliesTo(request))
            .ToList();

        var now = request.Now > clock.GetCurrentInstant() ? clock.GetCurrentInstant() : request.Now; 
        var pastTimestamps = request.Timestamps.Where(t => t <= now).ToList();
        var values = new Dictionary<Time, double>();
        if (pastTimestamps.Count > 0)
            values = (await defaultResolver.ResolveValues(request with { Timestamps = pastTimestamps })).ToDictionary(x => x.Key, x => x.Value);
        var futureTimestamps = request.Timestamps.Where(t => t > now).ToList();
        if (futureTimestamps.Count == 0)
            return values;
       
        // handle intraday and holidays
        var timestamp = now;
        if (!assetPair.SupportsIntraday || !now.IsWorkingDay(assetPair.HolidayCalendar))
            timestamp = timestamp.RollToWorkingDay("UK").CloseOfDay(); 

        var currentQuote = (await defaultResolver.ResolveValues(request with { Timestamps = new List<Time> { timestamp } })).Single().Value;
        foreach (var rule in rules)
        {
            var ruleValues = rule.Apply(request with { Timestamps = futureTimestamps, Now = timestamp }, currentQuote);
            foreach ( var t in futureTimestamps)
                values[t] = ruleValues[t];
        }

        return values;
    }
}