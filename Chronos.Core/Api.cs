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
        /// Get the url for asset pair quote value
        /// </summary>
        /// <param name="forAsset">Foreign asset</param>
        /// <param name="domAsset">Domestic asset</param>
        /// <returns>JSON data url</returns>
        static abstract string GetUrl(Asset forAsset, Asset domAsset);
        
        static abstract double GetValue(IJsonResult result, Asset domAsset);
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

                public static string GetUrl(string ticker) =>
                    $"https://eodhd.com/api/search/{ticker}?api_token={ApiKey ?? string.Empty}&fmt=json";

                public static IEnumerable<string> GetExchanges(IJsonResult result)
                {
                    var r = result as JsonResult;
                    return r?.Select(x => x.Exchange);
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
                
                public static string GetUrl(Asset forAsset, Asset domAsset)
                {
                    const string env = "real-time";
                    return $"https://eodhd.com/api/{env}/{forAsset.Ticker}-{domAsset.Ticker}.CC?from=$date&to=$date&fmt=json&api_token={ApiKey ?? string.Empty}";
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

                public static string GetUrl(Asset forAsset, Asset domAsset)
                {
                    const string env = "eod";
                    return $"https://eodhd.com/api/{env}/{forAsset.Ticker}-{domAsset.Ticker}.CC?from=$date&to=$date&fmt=json&api_token={ApiKey ?? string.Empty}";
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
                
                public static string GetUrl(Asset forAsset, Asset domAsset)
                {
                    const string env = "real-time";
                    return $"https://eodhd.com/api/{env}/{forAsset.Ticker}?from=$date&to=$date&fmt=json&api_token={ApiKey ?? string.Empty}";
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

                public static string GetUrl(Asset forAsset, Asset domAsset)
                {
                    const string env = "eod";
                    return $"https://eodhd.com/api/{env}/{forAsset.Ticker}?from=$date&to=$date&fmt=json&api_token={ApiKey ?? string.Empty}";
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
                
                public static string GetUrl(Asset forAsset, Asset domAsset)
                {
                    const string env = "real-time";
                    return $"https://eodhd.com/api/{env}/{forAsset.Ticker}{domAsset.Ticker}.FOREX?from=$date&to=$date&fmt=json&api_token={ApiKey ?? string.Empty}";
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

                public static string GetUrl(Asset forAsset, Asset domAsset)
                {
                    const string env = "eod";
                    return $"https://eodhd.com/api/{env}/{forAsset.Ticker}{domAsset.Ticker}.FOREX?from=$date&to=$date&fmt=json&api_token={ApiKey ?? string.Empty}";
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
        public static class FxApiLayer
        {
            private static string ApiKey => Environment.GetEnvironmentVariable("FX_APIKEY");

            public class JsonResult : IJsonQuoteResult
            {
                public Rates Rates { get; set; }
                public string RequestorId { get; set; }
                public bool Success { get; set; }
                public static string GetDateFormat() => "yyyy-MM-dd";

                public static string GetUrl(Asset forAsset, Asset domAsset)
                {
                    return $"https://api.apilayer.com/exchangerates_data/$date?symbols={domAsset.Ticker}&base={forAsset.Ticker}" + (ApiKey != null ? $";{ApiKey}" : string.Empty);
                }

                public static double GetValue(IJsonResult result, Asset domAsset)
                {
                    var r = result as JsonResult;
                    return r?.Rates.GetType().GetProperty(domAsset.Ticker)?.GetValue(r.Rates) as double? ?? 0;
                }
            }

            public class Rates
            {
                public double USD { get; set; }
                public double GBP { get; set; }
            }
        }

        /// <summary>
        /// Crypto coin JSON Api
        /// </summary>
        public static class CoinGecko
        {
            public class JsonResult : IJsonQuoteResult
            {
                public string Id { get; set; }
                public string Symbol { get; set; } 
                public string Name { get; set; } 
                public MarketData Market_Data { get; set; } 
                public string RequestorId { get; set; }
                
                public static string GetDateFormat() => "dd-MM-yyyy";
                
                public static string GetUrl(Asset forAsset, Asset domAsset)
                {
                    if (domAsset.Ticker != "USD")
                        throw new InvalidOperationException("Only USD is supported as domestic currency");
                    var url = $"https://api.coingecko.com/api/v3/coins/{forAsset.AssetId.ToLower()}/history?date=$date&localization=false";
                    return url;
                }

                public static double GetValue(IJsonResult result, Asset domAsset)
                {
                    return (result as JsonResult)?.Market_Data?.Current_price?.Usd ?? 0;    
                }
            }
            
            public class MarketData
            {
                public CurrentPrice Current_price { get; set; } 
            }
            
            public class CurrentPrice
            {
                public double Usd { get; set; }
            }
        }
    }
}