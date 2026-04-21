using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly Dictionary<Asset, double> _costBasis = new();
        private readonly Dictionary<Asset, List<(Quantity quantity, Time timestamp)>> _deposits = new();
        private readonly Dictionary<Asset, List<(Quantity assetQuantity, Quantity quantity, Time timestamp)>> _costs = new();
        private readonly Dictionary<Asset, List<(Time timestamp, double ratio )>> _splits = new();
        
        private Dictionary<Asset, double> _positions = new();

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
            _costs = new Dictionary<Asset, List<(Quantity assetQuantity, Quantity quantity, Time timestamp)>>(other._costs);
            Transactions = new HashSet<string>(other.Transactions);
        }

        public Dictionary<Asset, double> Positions
        {
            get
            {
                if(_positions.Count > 0 || _deposits.Count == 0)
                    return _positions;

                _positions = GetPositions(Time.MaxValue);   
                return _positions;
            }    
        }
        
        public Dictionary<Asset, double> CostBasis
        {
            get
            {
                if(_costBasis.Count > 0)
                    return _costBasis;
                
                foreach(var asset in _deposits.Keys.Except(_costBasis.Keys))
                    _costBasis[asset] = 0;
                
                if(_costs.Count == 0)
                    return _costBasis;

                foreach (var (asset, costs) in _costs)
                {
                    var costBasis = 0.0;
                    foreach (var (q, c, t) in costs.OrderBy(x => x.timestamp))
                    {
                        switch (q.Amount)
                        {
                            // purchase
                            case >= 0:
                                costBasis += c.Amount;
                                break;
                            // sale
                            case < 0:
                            {
                                var positions = GetPositions(t);
                                if (positions.TryGetValue(asset, out var position))
                                {
                                    var costPrice = costBasis / position;
                                    costBasis += costPrice * q.Amount;
                                }

                                break;
                            }
                        }
                    }

                    _costBasis[asset] = costBasis;
                }
                
                return _costBasis;
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

        public void AddCost(Quantity assetQuantity, Quantity quantity, Time timestamp)
        {
            if(!_costs.ContainsKey(assetQuantity.Denominator))
                _costs[assetQuantity.Denominator] = new List<(Quantity assetQuantity, Quantity quantity, Time timestamp)>();
            
            _costs[assetQuantity.Denominator].Add((assetQuantity, quantity, timestamp));
            _costBasis.Clear();
        }
        
        private Dictionary<Asset, double> GetPositions(Time T)
        {
            var positions = new Dictionary<Asset, double>();
            foreach (var (asset, deposits) in _deposits)
            {
                var position = 0.0;
                var splits = _splits.GetValueOrDefault(asset, new());

                foreach (var (q, t) in deposits.Where(d => d.timestamp < T).OrderBy(d => d.timestamp))
                {
                    var amount = q.Amount;
                    foreach (var (s, r) in splits.Where(s => s.timestamp > t))
                        amount *= r;
                    position += amount;
                }
                    
                if (position != 0.0)
                    positions[asset] = position;
            }


            return positions;
        }
    }
}