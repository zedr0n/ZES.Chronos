using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Chronos.Core;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries
{
    public class AccountStatsState : ISingleState
    {
        private readonly Dictionary<Asset, double> _positions = new Dictionary<Asset, double>();

        public IEnumerable<Asset> Assets => _positions.Keys;
        public IEnumerable<double> Quantities => _positions.Values;

        public AccountStatsState()
        {
            
        }
        
        public AccountStatsState(AccountStatsState other)
        {
            _positions = new Dictionary<Asset, double>(other._positions);
        }

        public void Add(Asset asset, double amount)
        {
            if (_positions.ContainsKey(asset))
                _positions[asset] += amount;
            else
                _positions[asset] = amount;
        }
    }
}