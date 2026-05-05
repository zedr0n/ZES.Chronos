using System;
using System.Collections.Generic;
using System.Linq;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Clocks;

namespace Chronos.Accounts;

public class UkAssetPools : IAssetPools
{
    private readonly List<Pool> _pools;
    private readonly Pool _sameDayAcquisitions = new();
    private readonly Pool _sameDayDisposals = new();
    // pending disposals by age
    private readonly List<Pool> _pendingDisposalsByAge;
    private readonly Pool _section104Pool = new();
    
    private DateTime _lastEndOfDay;
    private readonly Dictionary<int, double> _realisedGains = new();
    
    public double CostBasis
    {
        get
        {
            if (_lastEndOfDay == default)
                return 0.0;
            
            var pools = new UkAssetPools(this);
            pools.EndOfDay(_lastEndOfDay.AddDays(_pendingDisposalsByAge.Count+1).ToTime());
            return pools._pools.Sum(p => p.Cost);
        }
    }

    public double RealisedGain
    {
        get
        {
            if (_lastEndOfDay == default)
                return 0.0;
            
            var pools = new UkAssetPools(this);
            pools.EndOfDay(_lastEndOfDay.AddDays(_pendingDisposalsByAge.Count+1).ToTime());
            return pools._realisedGains.Sum(g => g.Value);
        }
    }
    
    /// <inheritdoc />
    public Dictionary<int, double> GetRealisedGainsPerTaxYear()
    {
        if (_lastEndOfDay == default)
            return new Dictionary<int, double>();
            
        var pools = new UkAssetPools(this);
        pools.EndOfDay(_lastEndOfDay.AddDays(_pendingDisposalsByAge.Count+1).ToTime());

        return new Dictionary<int, double>(pools._realisedGains);
    } 

    public double TotalQuantity
    {
        get
        {
            if (_lastEndOfDay == default)
                return 0.0;
            
            var pools = new UkAssetPools(this);
            pools.EndOfDay(_lastEndOfDay.AddDays(_pendingDisposalsByAge.Count+1).ToTime());
            return pools._pools.Sum(p => p.Quantity);
        }
    } 

    public UkAssetPools(int numberOfMatchingDays = 30)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(numberOfMatchingDays);

        _pendingDisposalsByAge = Enumerable.Range(0, numberOfMatchingDays).Select(_ => new Pool()).ToList();
        _pools =
        [
            _sameDayAcquisitions,
            _sameDayDisposals
        ];
        _pools.AddRange(_pendingDisposalsByAge);
        _pools.Add(_section104Pool);
    }

    public UkAssetPools(UkAssetPools other)
    {
        _lastEndOfDay = other._lastEndOfDay;
        _sameDayAcquisitions = new Pool(other._sameDayAcquisitions);
        _sameDayDisposals = new Pool(other._sameDayDisposals);
        _section104Pool = new Pool(other._section104Pool);
        _realisedGains = new Dictionary<int, double>(other._realisedGains);
        _pendingDisposalsByAge = other._pendingDisposalsByAge.Select(p => new Pool(p)).ToList();
        _pools =
        [
            _sameDayAcquisitions,
            _sameDayDisposals
        ];
        _pools.AddRange(_pendingDisposalsByAge);
        _pools.Add(_section104Pool);
    }

    public void TransferFrom(IAssetPools source, double quantity)
    {
        var s = (UkAssetPools)source;
        
        if(_pendingDisposalsByAge.Count != s._pendingDisposalsByAge.Count)
            throw new ArgumentException("Source and target asset pools must have the same number of pending disposals by age");
        
        var effectivePosition = s._pools.Sum(p => p.Quantity); 
        var ratio = effectivePosition > 0 ? quantity / effectivePosition : 0;

        foreach (var (targetPool, sourcePool) in _pools.Zip(s._pools))
        {
            targetPool.Quantity += ratio * sourcePool.Quantity;
            targetPool.Cost += ratio * sourcePool.Cost;
            targetPool.Date = sourcePool.Date;
        }

        foreach (var v in s._realisedGains)
        {
            _realisedGains.TryAdd(v.Key, 0.0);
            _realisedGains[v.Key] += ratio * v.Value;
        }
    }

    public void TransferOut(double quantity)
    {
        var effectivePosition = _pools.Sum(p => p.Quantity); 
        var ratio = effectivePosition > 0 ? quantity / effectivePosition : 0;

        foreach (var pool in _pools)
        {
            pool.Quantity -= ratio * pool.Quantity;
            pool.Cost -= ratio * pool.Cost;
        }
        
        foreach (var key in _realisedGains.Keys.ToList())
            _realisedGains[key] -= ratio * _realisedGains[key];        
    }

    public void Acquire(Time time, double quantity, double cost)
    {
        _sameDayAcquisitions.Date = time.ToDateTime().Date;
        
        _sameDayAcquisitions.Quantity += quantity;
        _sameDayAcquisitions.Cost += cost;
    }

    public void Dispose(Time time, double quantity, double cost)
    {
        _sameDayDisposals.Date = time.ToDateTime().Date; 
        _realisedGains.TryAdd(_sameDayDisposals.TaxYear, 0.0);
        
        _sameDayDisposals.Quantity -= quantity;
        _sameDayDisposals.Cost -= cost;
    }

    public void EndOfDay(Time time)
    {
        var date = time.ToDateTime().Date;
        if (_lastEndOfDay == default)
            _lastEndOfDay = date.AddDays(-1);
        
        if (date == _lastEndOfDay) 
            return;

        var closeSameDay = true;
        
        // AccountStatsQueryHandler advances pools on every transaction date. If a later query skips
        // more than the matching window, no unprocessed acquisition can still match these
        // pending disposals, so they can be closed before jumping to the final ageing window.
        if (_lastEndOfDay < date.AddDays(-_pendingDisposalsByAge.Count) && _pendingDisposalsByAge.Count > 0)
        {
            EndSingleDay(true);
            closeSameDay = false;

            if (_lastEndOfDay < date.AddDays(-_pendingDisposalsByAge.Count))
            {
                ClosePendingPools();
                _lastEndOfDay = date.AddDays(-_pendingDisposalsByAge.Count);
            }
        }

        while (_lastEndOfDay < date)
        {
            EndSingleDay(closeSameDay);
            closeSameDay = false;
        }
    }
    
    private void EndSingleDay(bool closeSameDay)
    {
        var remainingAcquisitions = closeSameDay ? _sameDayAcquisitions.Quantity : 0;
        var remainingDisposals = closeSameDay ? _sameDayDisposals.Quantity : 0;
        var sameDayDisposalsAverageCost = closeSameDay ? _sameDayDisposals.AverageCost : 0;
        var sameDayAcquisitionsAverageCost = closeSameDay ? _sameDayAcquisitions.AverageCost : 0;
        var section104AverageCost = _section104Pool.AverageCost;
        
        // same-day matched gain
        var matched = Math.Min(remainingAcquisitions, -remainingDisposals);
        if (matched > 0)
        {
            _realisedGains[_sameDayDisposals.TaxYear] += matched*(sameDayDisposalsAverageCost - sameDayAcquisitionsAverageCost);
            
            remainingAcquisitions -= matched;
            remainingDisposals += matched;
        }
        
        // do matching days rule
        for(var i = _pendingDisposalsByAge.Count - 1; i >= 0; i--)
        {
            if (remainingAcquisitions == 0)
                break;
            
            var pool = _pendingDisposalsByAge[i];
            var r = Math.Min(remainingAcquisitions, -pool.Quantity);
            if (r == 0) 
                continue;
            
            _realisedGains[pool.TaxYear] += r*(pool.AverageCost - sameDayAcquisitionsAverageCost);
            //_realisedGain += r*(pool.AverageCost - sameDayAcquisitionsAverageCost);
            
            remainingAcquisitions -= r;
            pool.Cost += r*pool.AverageCost;
            pool.Quantity += r;
        }
        
        // age the pending disposals
        var lastDisposalPool = new Pool(_pendingDisposalsByAge.LastOrDefault() ?? new Pool() { Quantity = remainingDisposals, Cost = remainingDisposals*sameDayDisposalsAverageCost, Date = _sameDayDisposals.Date });
        for(var i = _pendingDisposalsByAge.Count - 1; i > 0; i--)
        {
            var nextPool = _pendingDisposalsByAge[i];
            var pool = _pendingDisposalsByAge[i-1];
            
            nextPool.Quantity = pool.Quantity;
            nextPool.Cost = pool.Cost;
            nextPool.Date = pool.Date;
        }
        
        // move remaining same-day disposals to zero age pending pool
        if (_pendingDisposalsByAge.Count > 0)
        {
            _pendingDisposalsByAge[0].Date = remainingDisposals != 0 ? _sameDayDisposals.Date : DateTime.MinValue;
            _pendingDisposalsByAge[0].Quantity = remainingDisposals;
            _pendingDisposalsByAge[0].Cost = remainingDisposals*sameDayDisposalsAverageCost;
        }
        
        // remaining disposals come from S104
        if (lastDisposalPool.Quantity < 0)
        {
            //_realisedGain += -lastDisposalPool.Quantity*(lastDisposalPool.AverageCost - section104AverageCost);
            _realisedGains[lastDisposalPool.TaxYear] += -lastDisposalPool.Quantity*(lastDisposalPool.AverageCost - section104AverageCost);
            _section104Pool.Date = lastDisposalPool.Date;
            _section104Pool.Cost += lastDisposalPool.Quantity*section104AverageCost;
            _section104Pool.Quantity += lastDisposalPool.Quantity;
        }
        
        // remaining acquisitions go to section 104
        if (remainingAcquisitions > 0)
        {
            _section104Pool.Date = _sameDayAcquisitions.Date;
            _section104Pool.Cost += remainingAcquisitions*sameDayAcquisitionsAverageCost;
            _section104Pool.Quantity += remainingAcquisitions;
        }

        _lastEndOfDay = _lastEndOfDay.AddDays(1);
        
        if (!closeSameDay)
            return;
        
        _sameDayAcquisitions.Quantity = 0;
        _sameDayDisposals.Quantity = 0;
        _sameDayAcquisitions.Cost = 0;
        _sameDayDisposals.Cost = 0;
    }

    private void ClosePendingPools()
    {
        var section104AverageCost = _section104Pool.AverageCost;

        if (_sameDayAcquisitions.Quantity != 0)
        {
            _section104Pool.Cost += _sameDayAcquisitions.Cost;
            _section104Pool.Quantity += _sameDayAcquisitions.Quantity;
        
            _sameDayAcquisitions.Quantity = 0;
            _sameDayAcquisitions.Cost = 0;
        }

        if (_sameDayDisposals.Quantity != 0)
        {
            _realisedGains[_sameDayDisposals.TaxYear] += -_sameDayDisposals.Quantity*(_sameDayDisposals.AverageCost - section104AverageCost); 
            _section104Pool.Cost += _sameDayDisposals.Quantity*_section104Pool.AverageCost;
            _section104Pool.Quantity += _sameDayDisposals.Quantity;
            _sameDayDisposals.Quantity = 0;
            _sameDayDisposals.Cost = 0;
        }
        
        foreach (var pool in _pendingDisposalsByAge.Where(pool => pool.Quantity != 0))
        {
            _realisedGains[pool.TaxYear] += -pool.Quantity*(pool.AverageCost - section104AverageCost);
            _section104Pool.Cost += pool.Quantity*section104AverageCost;
            _section104Pool.Quantity += pool.Quantity;
            pool.Quantity = 0;
            pool.Cost = 0;
        }
    }
    
    private record Pool
    {
        public double Quantity { get; set; }
        public double Cost { get; set; }
        public double AverageCost => Quantity == 0 ? 0.0 : Cost / Quantity;

        public DateTime Date { get; set; } = DateTime.MinValue;
        public int TaxYear => GetTaxYear(Date);
        
        private static int GetTaxYear(DateTime date)
        {
            if(date == DateTime.MinValue)
                return 0;
            
            var year = date.Year;
            if (date.Month < 4 || date is { Month: 4, Day: < 6 })
                return year - 1;
            return year;
        }
        
        public Pool(Pool other)
        {
            if (other == null)
                return;
            
            Date = other.Date;
            Quantity = other.Quantity;
            Cost = other.Cost;
        }
    }
}
