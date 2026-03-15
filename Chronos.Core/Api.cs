using System;
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
        
        /// <summary>
        /// FX JSON Api 
        /// </summary>
        public static class Fx
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
        public static class Coin
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