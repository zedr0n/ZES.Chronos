using System;
using ZES.Interfaces.Net;

namespace Chronos.Core
{
    public static class Api
    {
        public static class Fx
        {
            public static string DateFormat => "yyyy-MM-dd";

            public static string Url(Asset forCurrency, Asset domCurrency)
            {
                if (domCurrency.Ticker != "USD")
                    throw new InvalidOperationException("Only USD is supported as domestic currency");
                return $"https://api.exchangeratesapi.io/$date?symbols={domCurrency.Ticker}&base={forCurrency.Ticker}";
            }

            public class JsonResult : IJsonResult
            {
                public Rates Rates { get; set; }
                public string RequestorId { get; set; }
            }

            public class Rates
            {
                public double USD { get; set; }
            }
        }

        public static class Coin
        {
            public static string DateFormat => "dd-MM-yyyy";
            
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