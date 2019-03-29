﻿using System;
using ZES.Infrastructure.Domain;

namespace Coins.Events
{
    public class CoinCreated : Event
    {
        public string CoinId { get; set; }
        public string Ticker { get; set; }
        public string Name { get; set; }
    }
}