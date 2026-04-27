using System;
using System.Collections.Generic;
using System.Linq;
using ZES.Interfaces.Clocks;

namespace Chronos.Accounts.Queries;

public class UkAssetPools : IAssetPools
{
    private readonly List<Pool> _pools;
    private readonly Pool _sameDayAcquisitions = new();
    private readonly Pool _sameDayDisposals = new();
    private readonly Pool _section104Pool = new();
    
    private DateTime _lastEndOfDay;
    private double _realisedGain;

    public double CostBasis
    {
        get
        {
            var pools = new UkAssetPools(this);
            pools.EndOfDay(Time.MaxValue);
            return pools._pools.Sum(p => p.Cost);
        }
    }

    public double RealisedGain
    {
        get
        {
            var pools = new UkAssetPools(this);
            pools.EndOfDay(Time.MaxValue);
            return pools._realisedGain;
        }
    }
    public double TotalQuantity => _pools.Sum(p => p.Quantity); 

    public UkAssetPools()
    {
        _pools = [_sameDayAcquisitions, _sameDayDisposals, _section104Pool];
    }

    public UkAssetPools(UkAssetPools other)
    {
        _lastEndOfDay = other._lastEndOfDay;
        _sameDayAcquisitions = new Pool(other._sameDayAcquisitions);
        _sameDayDisposals = new Pool(other._sameDayDisposals);
        _section104Pool = new Pool(other._section104Pool);
        _realisedGain = other._realisedGain;
        _pools = [_sameDayAcquisitions, _sameDayDisposals, _section104Pool];
    }

    public void TransferFrom(IAssetPools source, double quantity)
    {
        var s = (UkAssetPools)source;
        var effectivePosition = s._section104Pool.Quantity + s._sameDayAcquisitions.Quantity - s._sameDayDisposals.Quantity;
        var ratio = effectivePosition > 0 ? quantity / effectivePosition : 0;

        foreach (var (targetPool, sourcePool) in _pools.Zip(s._pools))
        {
            targetPool.Quantity += ratio * sourcePool.Quantity;
            targetPool.Cost += ratio * sourcePool.Cost;
        }
    }

    public void TransferOut(double quantity)
    {
        var effectivePosition = _section104Pool.Quantity + _sameDayAcquisitions.Quantity - _sameDayDisposals.Quantity;
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
        _sameDayDisposals.Quantity += quantity;
        _sameDayDisposals.Cost += cost;
    }

    public void EndOfDay(Time time)
    {
        var date = time.ToDateTime().Date;
        if (date == _lastEndOfDay) 
            return;
        
        // same-day matched gain
        var matched = Math.Min(_sameDayAcquisitions.Quantity, _sameDayDisposals.Quantity); 
        _realisedGain += matched*(_sameDayDisposals.AverageCost - _sameDayAcquisitions.AverageCost);
        
        // remaining disposals come from S104
        var netDisposals = _sameDayDisposals.Quantity - matched;
        _realisedGain += netDisposals*(_sameDayDisposals.AverageCost - _section104Pool.AverageCost);
        _section104Pool.Cost -= netDisposals*_section104Pool.AverageCost;
        _section104Pool.Quantity -= netDisposals;
        
        // remaining acquisitions go to section 104
        var netAcquisitions = _sameDayAcquisitions.Quantity - matched;
        _section104Pool.Cost += netAcquisitions*_sameDayAcquisitions.AverageCost;
        _section104Pool.Quantity += netAcquisitions;
       
        _sameDayAcquisitions.Quantity = 0;
        _sameDayDisposals.Quantity = 0;
        _sameDayAcquisitions.Cost = 0;
        _sameDayDisposals.Cost = 0;
        
        _lastEndOfDay = date;
    }
    
    private record Pool
    {
        public double Quantity { get; set; }
        public double Cost { get; set; }
        public double AverageCost => Quantity == 0 ? 0.0 : Cost / Quantity;

        public Pool(Pool other)
        {
            Quantity = other.Quantity;
            Cost = other.Cost;
        }
    }
}