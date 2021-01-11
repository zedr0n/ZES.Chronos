using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Chronos.Core;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries
{
    /// <summary>
    /// State for account stats
    /// </summary>
    public class AccountStatsState : ISingleState
    {
        private readonly Dictionary<Asset, double> _positions = new Dictionary<Asset, double>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountStatsState"/> class.
        /// </summary>
        public AccountStatsState()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountStatsState"/> class.
        /// </summary>
        /// <param name="other">Source state</param>
        public AccountStatsState(AccountStatsState other)
        {
            _positions = new Dictionary<Asset, double>(other._positions);
            Transactions = new HashSet<string>(other.Transactions);
        }
       
        /// <summary>
        /// Gets all account transactions
        /// </summary>
        public HashSet<string> Transactions { get; } = new HashSet<string>();
        
        /// <summary>
        /// Gets all account assets
        /// </summary>
        public IEnumerable<Asset> Assets => _positions.Keys;
        
        /// <summary>
        /// Gets all account quantities
        /// </summary>
        public IEnumerable<double> Quantities => _positions.Values;

        /// <summary>
        /// Add the asset to account
        /// </summary>
        /// <param name="asset">Asset to add</param>
        /// <param name="amount">Amount of added asset</param>
        public void Add(Asset asset, double amount)
        {
            if (_positions.ContainsKey(asset))
                _positions[asset] += amount;
            else
                _positions[asset] = amount;
        }
    }
}