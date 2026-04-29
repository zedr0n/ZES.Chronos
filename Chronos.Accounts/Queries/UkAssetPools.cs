using System;
using System.Collections.Generic;
using System.Linq;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Clocks;

namespace Chronos.Accounts.Queries;

public class UkAssetPools : IAssetPools
{
    private readonly List<Pool> _pools;
    private readonly Pool _sameDayAcquisitions = new();
    private readonly Pool _sameDayDisposals = new();
    // pending disposals by age
    private readonly List<Pool> _pendingDisposalsByAge;
    private readonly Pool _section104Pool = new();
    
    private DateTime _lastEndOfDay;
    private double _realisedGain;
    
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
            return pools._realisedGain;
        }
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
        _realisedGain = other._realisedGain;
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
    }

    public void Acquire(Time time, double quantity, double cost)
    {
       _sameDayAcquisitions.Quantity += quantity;
       _sameDayAcquisitions.Cost += cost;
    }

    public void Dispose(Time time, double quantity, double cost)
    {
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
            _realisedGain += matched*(sameDayDisposalsAverageCost - sameDayAcquisitionsAverageCost);
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
            
            _realisedGain += r*(pool.AverageCost - sameDayAcquisitionsAverageCost);
            
            remainingAcquisitions -= r;
            pool.Cost += r*pool.AverageCost;
            pool.Quantity += r;
        }
        
        // age the pending disposals
        var lastDisposalPool = new Pool(_pendingDisposalsByAge.LastOrDefault() ?? new Pool() { Quantity = remainingDisposals, Cost = remainingDisposals*sameDayDisposalsAverageCost });
        for(var i = _pendingDisposalsByAge.Count - 1; i > 0; i--)
        {
            var nextPool = _pendingDisposalsByAge[i];
            var pool = _pendingDisposalsByAge[i-1];
            
            nextPool.Quantity = pool.Quantity;
            nextPool.Cost = pool.Cost;
        }
        
        // move remaining same-day disposals to zero age pending pool
        if (_pendingDisposalsByAge.Count > 0)
        {
            _pendingDisposalsByAge[0].Quantity = remainingDisposals;
            _pendingDisposalsByAge[0].Cost = remainingDisposals*sameDayDisposalsAverageCost;
        }
        
        // remaining disposals come from S104
        if (lastDisposalPool.Quantity < 0)
        {
            _realisedGain += -lastDisposalPool.Quantity*(lastDisposalPool.AverageCost - section104AverageCost);
            _section104Pool.Cost += lastDisposalPool.Quantity*section104AverageCost;
            _section104Pool.Quantity += lastDisposalPool.Quantity;
        }
        
        // remaining acquisitions go to section 104
        if (remainingAcquisitions > 0)
        {
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
        
        _section104Pool.Cost += _sameDayAcquisitions.Cost;
        _section104Pool.Quantity += _sameDayAcquisitions.Quantity;
        
        _sameDayAcquisitions.Quantity = 0;
        _sameDayAcquisitions.Cost = 0;
        
        _realisedGain += -_sameDayDisposals.Quantity*(_sameDayDisposals.AverageCost - section104AverageCost); 
        _section104Pool.Cost += _sameDayDisposals.Quantity*_section104Pool.AverageCost;
        _section104Pool.Quantity += _sameDayDisposals.Quantity;
        _sameDayDisposals.Quantity = 0;
        _sameDayDisposals.Cost = 0;
        
        foreach (var pool in _pendingDisposalsByAge.Where(pool => pool.Quantity != 0))
        {
            _realisedGain += -pool.Quantity*(pool.AverageCost - section104AverageCost);
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

        public Pool(Pool other)
        {
            if (other == null)
                return;
            
            Quantity = other.Quantity;
            Cost = other.Cost;
        }
    }
}
