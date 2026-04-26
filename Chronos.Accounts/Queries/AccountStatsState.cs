using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Chronos.Core;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries
{
    /// <summary>
    /// State for account stats
    /// </summary>
    public class AccountStatsState : IState
    {
        private readonly Dictionary<Asset, List<(Quantity quantity, Time timestamp)>> _deposits = new();
        private readonly Dictionary<Time, List<(Quantity assetQuantity, Quantity costQuantity)>> _costs = new();
        private readonly Dictionary<Asset, List<(Time timestamp, double ratio )>> _splits = new();
        
        private Dictionary<Asset, double> _positions = new();

        public ReadOnlyDictionary<Time, List<(Quantity assetQuantity, Quantity costQuantity)>> Costs =>
            _costs.AsReadOnly();

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
            _deposits = new Dictionary<Asset, List<(Quantity quantity, Time timestamp)>>(other._deposits);
            _splits = new Dictionary<Asset, List<(Time timestamp, double ratio)>>(other._splits);
            _costs = new Dictionary<Time, List<(Quantity assetQuantity, Quantity costQuantity)>>(other._costs);
            Transactions = new HashSet<string>(other.Transactions);
        }

        public Dictionary<Asset, double> Positions
        {
            get
            {
                if(_positions.Count > 0 || _deposits.Count == 0)
                    return _positions;

                _positions = GetPositions(Time.MaxValue).Value;   
                return _positions;
            }    
        }

        /// <summary>
        /// Gets all account transactions
        /// </summary>
        public HashSet<string> Transactions { get; } = new();
        
        /// <summary>
        /// Gets all account assets
        /// </summary>
        public IEnumerable<Asset> Assets => Positions.Keys;
        
        /// <summary>
        /// Gets all account quantities
        /// </summary>
        public IEnumerable<double> Quantities => Positions.Values;

        /// <summary>
        /// Adds a quantity deposit along with its timestamp to the state.
        /// </summary>
        /// <param name="quantity">The quantity being deposited.</param>
        /// <param name="timestamp">The time of the deposit.</param>
        public void Add(Quantity quantity, Time timestamp)
        {
            if (!_deposits.ContainsKey(quantity.Denominator))
                _deposits[quantity.Denominator] = new List<(Quantity quantity, Time timestamp)>();
            
            _deposits[quantity.Denominator].Add((quantity, timestamp));
            //if( _splits.ContainsKey(quantity.Denominator))
            _positions.Clear();
        }

        /// <summary>
        /// Adds a split for a specific asset, including the timestamp and ratio of the split.
        /// </summary>
        /// <param name="asset">The asset for which the split is being added.</param>
        /// <param name="timestamp">The time at which the split occurred.</param>
        /// <param name="ratio">The ratio by which the asset is split.</param>
        public void AddSplit(Asset asset, Time timestamp, double ratio)
        {
            if (!_splits.ContainsKey(asset))
                _splits[asset] = new List<(Time timestamp, double ratio)>();
            _splits[asset].Add((timestamp, ratio));
            _positions.Clear();
        }

        public void AddCost(Quantity assetQuantity, Quantity costQuantity, Time timestamp)
        {
            if(!_costs.ContainsKey(timestamp))
                _costs[timestamp] = new List<(Quantity assetQuantity, Quantity costQuantity)>();
           
            _costs[timestamp].Add((assetQuantity, costQuantity));
        }

        /// <summary>
        /// Computes the positions for each asset up to the specified time and returns a lazy-loaded dictionary of the results.
        /// </summary>
        /// <param name="T">The time up to which the positions are calculated.</param>
        /// <returns>A lazy-loaded dictionary mapping each asset to its computed position value.</returns>
        public Lazy<Dictionary<Asset, double>> GetPositions(Time T)
        {
            return new Lazy<Dictionary<Asset, double>>(() =>
            {
                var positions = new Dictionary<Asset, double>();
                foreach (var (asset, deposits) in _deposits)
                {
                    var position = 0.0;
                    var splits = _splits.GetValueOrDefault(asset, new());

                    foreach (var (q, t) in deposits.Where(d => d.timestamp <= T).OrderBy(d => d.timestamp))
                    {
                        var amount = q.Amount;
                        foreach (var (s, r) in splits.Where(s => s.timestamp > t))
                            amount *= r;
                        position += amount;
                    }

                    positions[asset] = position;
                }


                return positions;
            });
        }
    }
}