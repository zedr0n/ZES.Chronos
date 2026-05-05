using System;
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
        private readonly Dictionary<Time, List<Guid?>> _costCommandIds = new();
        private readonly Dictionary<Asset, List<(Time timestamp, double ratio)>> _splits = new();
        private readonly Dictionary<Time, List<(string fromAccount, string toAccount, Quantity quantity)>> _transfers = new();
        private readonly Dictionary<string, Dictionary<Time, double>> _quotes = new();
        private readonly HashSet<string> _accountNames = [];
        private readonly Dictionary<Time, List<(string fromAccount, Quantity fee)>> _feeDisposals = new();
        private readonly Dictionary<Time, List<Guid?>> _feeDisposalCommandIds = new();
       
        private readonly Dictionary<Asset, double> _positions = new();

        public ReadOnlyDictionary<Time, List<(Quantity assetQuantity, Quantity costQuantity)>> Costs =>
            _costs.AsReadOnly();

        public IEnumerable<(Quantity assetQuantity, Quantity costQuantity, Guid? commandId)> GetCosts(Time time)
        {
            var costs = _costs.GetValueOrDefault(time, []);
            var ids = _costCommandIds.GetValueOrDefault(time, []);
            
            return costs.Select((c, i) => (c.assetQuantity, c.costQuantity, i < ids.Count ? ids[i] : null));        
        }

        public Dictionary<Time, List<Quantity>> FeeDisposals => _feeDisposals
            .ToDictionary(t => t.Key, t => t.Value.Where(v => IsIncludedAccount(v.fromAccount)).Select(v => v.fee).ToList())
            .Where(kv => kv.Value.Count > 0)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        public IEnumerable<(Quantity feeQuantity, Guid? commandId)> GetFeeDisposals(Time time)
        {
            var feeDisposals = _feeDisposals.GetValueOrDefault(time, []);
            var ids = _feeDisposalCommandIds.GetValueOrDefault(time, []);

            return feeDisposals
                .Select((f, i) => (f.fromAccount, f.fee, commandId: i < ids.Count ? ids[i] : null))
                .Where(x => IsIncludedAccount(x.fromAccount))
                .Select(x => (x.fee, x.commandId));
        }
        
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
            AccountName = other.AccountName;
            _positions = new Dictionary<Asset, double>(other._positions);
            _deposits = other._deposits.ToDictionary(x => x.Key, x => x.Value.ToList());
            _splits = other._splits.ToDictionary(x => x.Key, x => x.Value.ToList());
            _costs = other._costs.ToDictionary(x => x.Key, x => x.Value.ToList());
            _costCommandIds = other._costCommandIds.ToDictionary(x => x.Key, x => x.Value.ToList());
            _transfers = other._transfers.ToDictionary(x => x.Key, x => x.Value.ToList());
            _quotes = other._quotes.ToDictionary(x => x.Key, x => new Dictionary<Time, double>(x.Value));
            _feeDisposals = other._feeDisposals.ToDictionary(x => x.Key, x => x.Value.ToList());
            _feeDisposalCommandIds = other._feeDisposalCommandIds.ToDictionary(x => x.Key, x => x.Value.ToList());
            _accountNames = new HashSet<string>(other.GetAccountNames());
            Transactions = new HashSet<string>(other.Transactions);
        }

        public AccountStatsState Copy() => new(this);
        
        public AccountStatsState CombineWith(AccountStatsState other)
        {
            if (other == null)
                return this;
            
            foreach (var accountName in GetAccountNames().Concat(other.GetAccountNames()).Where(a => a != null))
                _accountNames.Add(accountName);

            AddRange(_deposits, other._deposits);
            AddDistinctRange(_splits, other._splits);
            AddRange(_costs, other._costs);
            AddRange(_costCommandIds, other._costCommandIds);
            AddDistinctRange(_transfers, other._transfers);
            AddDistinctRange(_feeDisposals, other._feeDisposals);
            AddRange(_feeDisposalCommandIds, other._feeDisposalCommandIds);
            MergeQuotes(other._quotes);
           
            RemoveInternalTransfers();
            SortCombinedState();
            
            foreach (var txId in other.Transactions)
                Transactions.Add(txId);
            
            _positions.Clear();
            return this;
        }

        public Dictionary<Asset, double> Positions
        {
            get
            {
                if(_positions.Count > 0 || _deposits.Count == 0)
                    return _positions;

                foreach (var (asset, deposits) in _deposits)
                {
                    var position = 0.0;
                    var splits = _splits.GetValueOrDefault(asset, new());

                    foreach (var (q, t) in deposits.OrderBy(d => d.timestamp))
                    {
                        var amount = q.Amount;
                        foreach (var (s, r) in splits.Where(s => s.timestamp > t))
                            amount *= r;
                        position += amount;
                    }

                    _positions[asset] = position;
                }

                return _positions;
            }    
        }

        public string AccountName { get; set; }
        
        /// <summary>
        /// Gets all account transactions
        /// </summary>
        public HashSet<string> Transactions { get; } = new();

        /// <summary>
        /// Gets transfers associated with the account over time.
        /// </summary>
        public Dictionary<Time, List<Quantity>> AssetTransfers => _transfers
            .ToDictionary(t => t.Key, t => 
                t.Value.Where(v => IsIncludedAccount(v.toAccount) || IsIncludedAccount(v.fromAccount))
                    .Select(v => v.quantity * ( IsIncludedAccount(v.toAccount) ? 1 : -1 )).ToList())
            .Where(kv => kv.Value.Count > 0)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        public Dictionary<Time, List<(string fromAccount, Quantity quantity)>> GetAssetTransfersIn()
        {
            return _transfers
                .ToDictionary(t => t.Key, t => 
                    t.Value.Where(v => IsIncludedAccount(v.toAccount))
                        .Select(v => (v.fromAccount, v.quantity)).ToList())
                .Where(kv => kv.Value.Count > 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        
        public Dictionary<Time, List<Quantity>> GetAssetTransfersOut()
        {
            return _transfers
                .ToDictionary(t => t.Key, t => 
                    t.Value.Where(v => IsIncludedAccount(v.fromAccount))
                        .Select(v => v.quantity).ToList())
                .Where(kv => kv.Value.Count > 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        
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

        public void AddCost(Quantity assetQuantity, Quantity costQuantity, Quantity feeQuantity, Time timestamp, Guid? commandId = null)
        {
            _costs.TryAdd(timestamp, []);
            _costCommandIds.TryAdd(timestamp, []);

            if (double.IsNaN(costQuantity.Amount))
            {
                var fordom = AssetPair.Fordom(assetQuantity.Denominator, costQuantity.Denominator);
                if (_quotes.TryGetValue(fordom, out var quotes) && quotes.TryGetValue(timestamp, out var quote))
                    costQuantity = costQuantity with { Amount = assetQuantity.Amount * quote };
            }            
            
            if (feeQuantity != null && feeQuantity.IsValid())
                costQuantity += feeQuantity;
            
            _costs[timestamp].Add((assetQuantity, costQuantity));
            _costCommandIds[timestamp].Add(commandId);
        }

        public void AddQuote(string fordom, double quote, Time timestamp)
        {
            if(!_quotes.ContainsKey(fordom))
                _quotes[fordom] = new Dictionary<Time, double>();
            
            _quotes[fordom][timestamp] = quote;
            
            if (!_costs.TryGetValue(timestamp, out var costs)) return;
            for (var i = 0; i < costs.Count; i++)
            {
                var (q, c) = costs[i];
                if (!double.IsNaN(c.Amount)) 
                    continue;
                if (AssetPair.Fordom(q.Denominator, c.Denominator) != fordom)
                    continue;
                costs[i] = (q, c with { Amount = q.Amount * quote });
            }
        }

        public void AddAssetTransfer(string fromAccount, string toAccount, Quantity quantity, Quantity fee, Time timestamp)
        { 
            if (!_transfers.TryGetValue(timestamp, out var transfers))
            {
                transfers = new List<(string fromAccount, string toAccount, Quantity quantity)>();
                _transfers[timestamp] = transfers;
            }
            
            var transferQuantity = quantity.Copy();
            if (fee != null && fee.IsValid() && fee.Amount != 0 && fee.Denominator == quantity.Denominator)
                transferQuantity -= fee;

            var existingTransfer = transfers.FirstOrDefault(t =>
                t.fromAccount == fromAccount &&
                t.toAccount == toAccount &&
                transferQuantity.Denominator == t.quantity.Denominator);

            if (existingTransfer.quantity != null)
            {
                transfers.Remove(existingTransfer);
                transfers.Add((fromAccount, toAccount, transferQuantity + existingTransfer.quantity));
            }
            else
            {
                transfers.Add((fromAccount, toAccount, transferQuantity));
            }
        }

        public void AddFeeDisposal(string fromAccount, Quantity fee, Time timestamp, Guid? commandId)
        {
            _feeDisposals.TryAdd(timestamp, []);
            _feeDisposalCommandIds.TryAdd(timestamp, []);

            _feeDisposals[timestamp].Add((fromAccount, fee));
            _feeDisposalCommandIds[timestamp].Add(commandId);
        }

        private IEnumerable<string> GetAccountNames()
        {
            if (_accountNames.Count > 0)
                return _accountNames;

            return AccountName == null ? Enumerable.Empty<string>() : new[] { AccountName };
        }

        private bool IsIncludedAccount(string accountName)
        {
            if (_accountNames.Count > 0)
                return _accountNames.Contains(accountName);

            return accountName == AccountName;
        }

        private static void AddRange<TKey, TValue>(Dictionary<TKey, List<TValue>> target, Dictionary<TKey, List<TValue>> source)
        {
            foreach (var (key, values) in source)
            {
                if (!target.TryGetValue(key, out var targetValues))
                {
                    targetValues = new List<TValue>();
                    target[key] = targetValues;
                }

                targetValues.AddRange(values);
            }
        }

        private static void AddDistinctRange<TKey, TValue>(Dictionary<TKey, List<TValue>> target, Dictionary<TKey, List<TValue>> source)
        {
            foreach (var (key, values) in source)
            {
                if (!target.TryGetValue(key, out var targetValues))
                {
                    targetValues = new List<TValue>();
                    target[key] = targetValues;
                }

                foreach (var value in values)
                {
                    if (!targetValues.Contains(value))
                        targetValues.Add(value);
                }
            }
        }

        private void MergeQuotes(Dictionary<string, Dictionary<Time, double>> source)
        {
            foreach (var (assetPair, quotes) in source)
            {
                if (!_quotes.TryGetValue(assetPair, out var targetQuotes))
                {
                    targetQuotes = new Dictionary<Time, double>();
                    _quotes[assetPair] = targetQuotes;
                }

                foreach (var (time, quote) in quotes)
                    targetQuotes[time] = quote;
            }
        }

        private void SortCombinedState()
        {
            foreach (var deposits in _deposits.Values)
                deposits.Sort((x, y) => x.timestamp.CompareTo(y.timestamp));

            foreach (var splits in _splits.Values)
                splits.Sort((x, y) => x.timestamp.CompareTo(y.timestamp));
        }

        private void RemoveInternalTransfers()
        {
            foreach (var timestamp in _transfers.Keys.ToList())
            {
                _transfers[timestamp] = _transfers[timestamp]
                    .Where(v => IsIncludedAccount(v.toAccount) != IsIncludedAccount(v.fromAccount))
                    .ToList();

                if (_transfers[timestamp].Count == 0)
                    _transfers.Remove(timestamp);
            }
        }
    }
}
