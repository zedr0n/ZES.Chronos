﻿using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Commands
{
    public class CreateCoin : Command, ICreateCommand
    {
        public CreateCoin() { }
        public CreateCoin(string name, string ticker)
        {
            Name = name;
            Ticker = ticker;
        }
        
        public string Name { get; set; }
        
        public string Ticker { get; set; }

        public override string Target => Name;
    }
}