﻿using System;
using Coins.Events;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;

namespace Coins
{
    /// <summary>
    /// Crypto coin aggregate root
    /// </summary>
    public class Coin : EventSourced, IAggregate 
    {
        private string _ticker;
        private string _name;
        
        public Coin() {}
        public Coin(string coinId, string ticker,string name)
        {
            When(new CoinCreated
            {
                CoinId = coinId,
                Name = name,
                Ticker = ticker
            });
        }

        public void When(CoinCreated e)
        {
            Id = e.CoinId;
            _ticker = e.Ticker;
            _name = e.Name;
            base.When(e);
        }
    }
}