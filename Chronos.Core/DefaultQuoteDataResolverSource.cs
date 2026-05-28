using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chronos.Core.Net;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Clocks;

namespace Chronos.Core;

/// <summary>
/// Resolves quote data for given assets and timestamps using a predefined data source.
/// </summary>
public class DefaultQuoteDataResolverSource(
  IEnumerable<IQuoteJsonRequestExecutor> jsonRequestExecutors,
  IWebApiProvider webApiProvider,
  IClock clock) : IQuoteDataResolverSource
{
    /// <inheritdoc/>
    public int Priority { get; } = 0;

    /// <inheritdoc/>
    public bool CanResolve(QuoteDataRequest request)
    {
      var now = clock.GetCurrentInstant();
      return !request.Timestamps.Any(t => t.Date() > now.Date());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<Time, double>> ResolveValues(QuoteDataRequest request)
    {
        var now = clock.GetCurrentInstant();
        var timestampsList = request.Timestamps.ToList();
        var timestamp = timestampsList.Max();
        var intraday = timestamp.Date() == now.Date();
        
        var domAsset = request.DomAsset;
        var forAsset = request.ForAsset;
        
        var webQuoteApi = webApiProvider.GetQuoteApi(forAsset.AssetType, domAsset.AssetType, intraday);

        List<(Time Timestamp, double Value)> values = domAsset.AssetId switch
        {
            "GBX" when forAsset.AssetId == "GBP" => timestampsList.Select(t => (t, 100.0)).ToList(),
            "GBP" when forAsset.AssetId == "GBX" => timestampsList.Select(t => (t, 0.01)).ToList(),
            _ => (await QueryValues(AssetPair.Fordom(forAsset, domAsset), timestampsList, request.Ticker, request.UrlOverride, request.EnforceCache, request.UseBatching, webQuoteApi)).ToList(),
        };

        return values.ToDictionary(x => x.Timestamp, x => x.Value); 
    }
    
    private async Task<IEnumerable<(Time Timestamp, double Value)>> QueryValues(
        string fordom,
        IEnumerable<Time> timestamps,
        string ticker,
        string urlOverride,
        bool enforceCache,
        bool useBatching,
        IWebQuoteApi webQuoteApi)
    {
      var dates = timestamps.ToList(); 
      
      if (!webQuoteApi.SupportsMultipleTimestamps() || dates.Count == 1 || !useBatching)
      {
        var valuesList = new List<(Time Timestamp, double Value)>();
        foreach (var t in dates.OrderBy(t => t).ToList())
        {
          var value = await QueryValue(fordom, t, ticker, urlOverride, enforceCache, webQuoteApi);
          valuesList.Add((t, value));
        }

        return valuesList;
      }
      
      var timestamp = dates.Max();
      var noTimeSpecified = timestamp == timestamp.StartOfDay();
      
      var preciseUrl = urlOverride ?? webQuoteApi.GetPreciseUrl(ticker, dates, enforceCache);
      var url = urlOverride ?? webQuoteApi.GetUrl(ticker, dates, enforceCache);

      var values = dates.ToDictionary(x => x, x => double.NaN); 
      
      var executor = jsonRequestExecutors.FirstOrDefault(x => x.JsonResultType == webQuoteApi.GetJsonResultType());
      if (executor == null)
        throw new InvalidOperationException($"No executor found for {webQuoteApi.GetJsonResultType()}");
      
      if (preciseUrl != null && preciseUrl != url && !noTimeSpecified)
      {
        var preciseRequestorId = $"{fordom}-precise-{timestamp.ToUnixTimeSeconds()}";
        var preciseData = await executor.Request(preciseRequestorId, preciseUrl);
        var preciseResults = webQuoteApi.GetValues(preciseData);
        var valid = true;
        
        var preciseResultsByDay = preciseResults
          .GroupBy(x => x.Key.Date())
          .ToDictionary(g => g.Key, g => g.First().Value);
        
        foreach (var t in dates)
        {
          if (!preciseResults.TryGetValue(t, out var v) && !preciseResultsByDay.TryGetValue(t.Date(), out v))
            v = double.NaN;
          
          values[t] = v;
          valid &= !double.IsNaN(v);
        }
        
        if (valid)
          return values.Select(x => (x.Key, x.Value)).ToList();
      }
          
      var requestorId = $"{fordom}-{timestamp.ToUnixTimeSeconds()}";
      
      var data = await executor.Request(requestorId, url);
      var results = webQuoteApi.GetValues(data);
      var resultsByDay = results
        .GroupBy(x => x.Key.Date())
        .ToDictionary(g => g.Key, g => g.First().Value);
      
      foreach (var t in dates.Where(t => double.IsNaN(values[t])))
      {
        if (!results.TryGetValue(t, out var v) && !resultsByDay.TryGetValue(t.Date(), out v))
          v = double.NaN;
          
        values[t] = v;
      }
      
      return values.Select(x => (x.Key, x.Value)).ToList();
    }
    
    private async Task<double> QueryValue(
      string fordom, 
      Time timestamp,
      string ticker,
      string urlOverride,
      bool enforceCache,
      IWebQuoteApi webQuoteApi)
    {
      var noTimeSpecified = timestamp == timestamp.StartOfDay();
      
      var preciseUrl = urlOverride ?? webQuoteApi.GetPreciseUrl(ticker, timestamp, enforceCache);
      var url = urlOverride ?? webQuoteApi.GetUrl(ticker, timestamp, enforceCache);

      var value = 0.0;
      
      var executor = jsonRequestExecutors.FirstOrDefault(x => x.JsonResultType == webQuoteApi.GetJsonResultType());
      if (executor == null)
        throw new InvalidOperationException($"No executor found for {webQuoteApi.GetJsonResultType()}");
      
      if (preciseUrl != null && preciseUrl != url && !noTimeSpecified)
      {
        var preciseRequestorId = $"{fordom}-precise-{timestamp.ToUnixTimeSeconds()}";
        var preciseData = await executor.Request(preciseRequestorId, preciseUrl);
        value = webQuoteApi.GetValue(preciseData).Value;
        if (!double.IsNaN(value))
          return value;
      }
          
      var requestorId = $"{fordom}-{timestamp.ToUnixTimeSeconds()}";
      var data = await executor.Request(requestorId, url);
      value = webQuoteApi.GetValue(data).Value;

      return value;
    }
}