using System;
using ZES.Interfaces.Net;

#pragma warning disable SA1600

namespace Chronos.Core
{
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
            /// <summary>
            /// Gets date format string
            /// </summary>
            public static string DateFormat => "yyyy-MM-dd";

            public static string ApiKey => Environment.GetEnvironmentVariable("FX_APIKEY");
            
            /// <summary>
            /// Get the url for FORDOM fx
            /// </summary>
            /// <param name="forCurrency">Foreign currency</param>
            /// <param name="domCurrency">Domestic currency</param>
            /// <returns>JSON data url</returns>
            /// <exception cref="InvalidOperationException">Only USD is supported as domestic</exception>
            public static string Url(Asset forCurrency, Asset domCurrency)
            {
                if (domCurrency.Ticker != "USD")
                    throw new InvalidOperationException("Only USD is supported as domestic currency");
                return $"https://api.apilayer.com/exchangerates_data/$date?symbols={domCurrency.Ticker}&base={forCurrency.Ticker}" + (ApiKey != null ? $";{ApiKey}" : string.Empty);
            }

            public class JsonResult : IJsonResult
            {
                public Rates Rates { get; set; }
                public string RequestorId { get; set; }
                public bool Success { get; set; }
            }

            public class Rates
            {
                public double USD { get; set; }
            }
        }

        /// <summary>
        /// Crypto coin JSON Api
        /// </summary>
        public static class Coin
        {
            /// <summary>
            /// Gets date format string
            /// </summary>
            public static string DateFormat => "dd-MM-yyyy";

            /// <summary>
            /// Get the url for coin price in domestic asset 
            /// </summary>
            /// <param name="coin">Coin asset</param>
            /// <param name="domCurrency">Domestic currency</param>
            /// <returns>JSON data url</returns>
            /// <exception cref="InvalidOperationException">Only USD is supported as domestic</exception>
            public static string Url(Asset coin, Asset domCurrency)
            {
                if (domCurrency.Ticker != "USD")
                    throw new InvalidOperationException("Only USD is supported as domestic currency");
                var url = $"https://api.coingecko.com/api/v3/coins/{coin.AssetId.ToLower()}/history?date=$date&localization=false";
                return url;
            }
            
            public class JsonResult : IJsonResult
            {
                public string Id { get; set; }
                public string Symbol { get; set; } 
                public string Name { get; set; } 
                public MarketData Market_Data { get; set; } 
                public string RequestorId { get; set; }
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