using System;
using System.Collections.Generic;
using System.Linq;
using ZES.Interfaces.Net;

#pragma warning disable SA1600

namespace Chronos.Core
{
    /// <inheritdoc />
    public interface IJsonQuoteResult : IJsonResult
    {
        /// <summary>
        /// Gets date format string
        /// </summary>
        /// <returns>Date format string</returns>
        static abstract string GetDateFormat();

        /// <summary>
        /// Retrieves the URL for accessing JSON data associated with a specific ticker.
        /// </summary>
        /// <param name="ticker">The ticker for which the URL is being requested.</param>
        /// <param name="enforceCache">A boolean flag indicating whether caching should be enforced for the result.</param>
        /// <returns>The URL as a string for accessing the JSON data for the specified ticker.</returns>
        static abstract string GetUrl(string ticker, bool enforceCache = false);

        static abstract double GetValue(IJsonResult result, Asset domAsset);

        static abstract string GetSearchTicker(string fordom, Asset domAsset, Asset forAsset);
    }

    /// <summary>
    /// JSON Api static data
    /// </summary>
    public static class Api
    {
        /// <summary>
        /// Get the server url
        /// </summary>
        /// <param name="useRemote">Use remote (web) server</param>
        /// <param name="server">Server url</param>
        /// <returns>True if info is available</returns>
        public static bool TryGetServer(bool useRemote, out string server)
        {
            var envVariable = useRemote ? "REMOTESERVER" : "SERVER";
            server = Environment.GetEnvironmentVariable(envVariable);
            return server != null;
        }

        public static class TickerSearch
        {
            private static string ApiKey => Environment.GetEnvironmentVariable("EQUITY_APIKEY");

            public class JsonResult : List<JsonResult.ExchangeData>, IJsonResult
            {
                public string RequestorId { get; set; }

                public static string GetUrl(string ticker, bool enforceCache = false) =>
                    $"https://eodhd.com/api/search/{ticker}?api_token={ApiKey ?? string.Empty}&fmt=json";

                public static IEnumerable<string> GetExchanges(IJsonResult result)
                {
                    var r = result as JsonResult;
                    return r?.Select(x => x.Exchange);
                }

                public static string GetTicker(IJsonResult result)
                {
                    var r = result as JsonResult;
                    var data = r?.FirstOrDefault();
                    return $"{data?.Code}.{data?.Exchange}";
                }

                public class ExchangeData
                {
                    public string Code { get; set; }
                    public string Type { get; set; }
                    public string Currency { get; set; }
                    public string Exchange { get; set; }
                }
            }
        }
       
        /// <summary>
        /// Equity JSON Api 
        /// </summary>
        public static class Coin
        {
            private static string ApiKey => Environment.GetEnvironmentVariable("EQUITY_APIKEY");

            public class LiveJsonResult : IJsonQuoteResult
            {
                public string RequestorId { get; set; }
                
                public double Open { get; set; }
                public double High { get; set; }
                public double Low { get; set; }
                public double Close { get; set; }
                public double PreviousClose { get; set; }
                public double Change { get; set; }

                public static string GetDateFormat() => "yyyy-MM-dd";
                
                public static string GetSearchTicker(string fordom, Asset domAsset, Asset forAsset) => $"{forAsset.AssetId}-{domAsset.AssetId}";
                
                public static string GetUrl(string ticker, bool enforceCache = false)
                {
                    const string env = "real-time";
                    return
                        $"https://eodhd.com/api/{env}/{ticker}?&fmt=json&api_token={ApiKey ?? string.Empty}{(enforceCache ? string.Empty : ";nocache")}";
                }
                
                public static double GetValue(IJsonResult result, Asset domAsset)
                {
                    var r = result as LiveJsonResult;
                    return r?.Close ?? 0;
                }
            }
            
            public class JsonResult : List<JsonResult.PriceData>, IJsonQuoteResult
            {
                public string RequestorId { get; set; }
                public static string GetDateFormat() => "yyyy-MM-dd";

                public static string GetSearchTicker(string fordom, Asset domAsset, Asset forAsset) => $"{forAsset.AssetId}-{domAsset.AssetId}";
                
                public static string GetUrl(string ticker, bool enforceCache = false)
                {
                    const string env = "eod";
                    return $"https://eodhd.com/api/{env}/{ticker}?from=$date&to=$date&fmt=json&api_token={ApiKey ?? string.Empty}";
                }
                
                public static double GetValue(IJsonResult result, Asset domAsset)
                {
                    var r = result as JsonResult;
                    var data = r?.SingleOrDefault();
                    return data?.Close ?? 0;
                }

                public class PriceData
                {
                    public double Open { get; set; }
                    public double High { get; set; }
                    public double Low { get; set; }
                    public double Close { get; set; }
                }
            }
        }        
        
        /// <summary>
        /// Equity JSON Api 
        /// </summary>
        public static class Equity
        {
            private static string ApiKey => Environment.GetEnvironmentVariable("EQUITY_APIKEY");

            public class LiveJsonResult : IJsonQuoteResult
            {
                public string RequestorId { get; set; }
                
                public double Open { get; set; }
                public double High { get; set; }
                public double Low { get; set; }
                public double Close { get; set; }
                public double PreviousClose { get; set; }
                public double Change { get; set; }

                public static string GetDateFormat() => "yyyy-MM-dd";
               
                public static string GetSearchTicker(string fordom, Asset domAsset, Asset forAsset) => $"{forAsset.AssetId}";
                
                public static string GetUrl(string ticker, bool enforceCache = false)
                {
                    const string env = "real-time";
                    return $"https://eodhd.com/api/{env}/{ticker}?&fmt=json&api_token={ApiKey ?? string.Empty}{(enforceCache ? string.Empty : ";nocache")}";
                }
                
                public static double GetValue(IJsonResult result, Asset domAsset)
                {
                    var r = result as LiveJsonResult;
                    return r?.Close ?? 0;
                }
            }
            
            public class JsonResult : List<JsonResult.PriceData>, IJsonQuoteResult
            {
                public string RequestorId { get; set; }
                public static string GetDateFormat() => "yyyy-MM-dd";

                public static string GetSearchTicker(string fordom, Asset domAsset, Asset forAsset) => $"{forAsset.AssetId}";
                
                public static string GetUrl(string ticker, bool enforceCache = false)
                {
                    const string env = "eod";
                    return $"https://eodhd.com/api/{env}/{ticker}?from=$date&to=$date&fmt=json&api_token={ApiKey ?? string.Empty}";
                }

                public static double GetValue(IJsonResult result, Asset domAsset)
                {
                    var r = result as JsonResult;
                    var data = r?.SingleOrDefault();
                    return data?.Close ?? 0;
                }

                public class PriceData
                {
                    public double Open { get; set; }
                    public double High { get; set; }
                    public double Low { get; set; }
                    public double Close { get; set; }
                }
            }
        }
        
        /// <summary>
        /// FX JSON Api 
        /// </summary>
        public static class Fx
        {
            private static string ApiKey => Environment.GetEnvironmentVariable("EQUITY_APIKEY");

            public class LiveJsonResult : IJsonQuoteResult
            {
                public string RequestorId { get; set; }
                
                public double Open { get; set; }
                public double High { get; set; }
                public double Low { get; set; }
                public double Close { get; set; }
                public double PreviousClose { get; set; }
                public double Change { get; set; }
                public long Timestamp { get; set; }

                public static string GetDateFormat() => "yyyy-MM-dd";
                
                public static string GetSearchTicker(string fordom, Asset domAsset, Asset forAsset) => $"{forAsset.AssetId}{domAsset.AssetId}";
                
                public static string GetUrl(string ticker, bool enforceCache = false)
                {
                    const string env = "real-time";
                    return $"https://eodhd.com/api/{env}/{ticker}?&fmt=json&api_token={ApiKey ?? string.Empty}{(enforceCache ? string.Empty : ";nocache")}";
                }
                
                public static double GetValue(IJsonResult result, Asset domAsset)
                {
                    var r = result as LiveJsonResult;
                    return r?.Close ?? 0;
                }
            }
            
            public class JsonResult : List<JsonResult.PriceData>, IJsonQuoteResult
            {
                public string RequestorId { get; set; }
                public static string GetDateFormat() => "yyyy-MM-dd";

                public static string GetSearchTicker(string fordom, Asset domAsset, Asset forAsset) => $"{forAsset.AssetId}{domAsset.AssetId}";
                
                public static string GetUrl(string ticker, bool enforceCache = false)
                {
                    const string env = "eod";
                    return $"https://eodhd.com/api/{env}/{ticker}?from=$date&to=$date&fmt=json&api_token={ApiKey ?? string.Empty}";
                }

                public static double GetValue(IJsonResult result, Asset domAsset)
                {
                    var r = result as JsonResult;
                    var data = r?.SingleOrDefault();
                    return data?.Close ?? 0;
                }

                public class PriceData
                {
                    public double Open { get; set; }
                    public double High { get; set; }
                    public double Low { get; set; }
                    public double Close { get; set; }
                }
            }
        }
    }
}