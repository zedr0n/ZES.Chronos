using System;
using System.Collections;
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
    
    // itemised disposal gains
    private readonly List<DisposalGainItem> _disposalGains = new();
    private readonly List<DisposalGainItem> _aggregatedDisposalGains = new();
    
    private DateTime _lastEndOfDay;
    private long _disposalSequence;
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

    public IReadOnlyList<DisposalGainItem> GetDisposalGains(bool aggregated = true)
    {
        if (_lastEndOfDay == default)
            return []; 
            
        var pools = new UkAssetPools(this);
        pools.EndOfDay(_lastEndOfDay.AddDays(_pendingDisposalsByAge.Count+1).ToTime()); 
        return aggregated ? pools._aggregatedDisposalGains : pools._disposalGains;
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
        _disposalSequence = other._disposalSequence;
        _sameDayAcquisitions = new Pool(other._sameDayAcquisitions);
        _sameDayDisposals = new Pool(other._sameDayDisposals);
        _section104Pool = new Pool(other._section104Pool);
        _realisedGains = new Dictionary<int, double>(other._realisedGains);
        _disposalGains = new List<DisposalGainItem>(other._disposalGains);
        _aggregatedDisposalGains = new List<DisposalGainItem>(other._aggregatedDisposalGains);
        
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
            targetPool.Disposals.Add(ratio * sourcePool.Disposals);
            targetPool.Date = sourcePool.Date;
        }

        foreach (var v in s._realisedGains)
        {
            _realisedGains.TryAdd(v.Key, 0.0);
            _realisedGains[v.Key] += ratio * v.Value;
        }

        foreach (var v in s._disposalGains)
            _disposalGains.Add(new DisposalGainItem(v, ratio));
        
        foreach(var v in s._aggregatedDisposalGains)
            _aggregatedDisposalGains.Add(new DisposalGainItem(v, ratio));
    }

    public void TransferOut(double quantity)
    {
        var effectivePosition = _pools.Sum(p => p.Quantity); 
        var ratio = effectivePosition > 0 ? quantity / effectivePosition : 0;

        foreach (var pool in _pools)
        {
            pool.Quantity -= ratio * pool.Quantity;
            pool.Cost -= ratio * pool.Cost;
            pool.Disposals.ReplaceWith(pool.Disposals * (1.0-ratio));
        }
        
        foreach (var key in _realisedGains.Keys.ToList())
            _realisedGains[key] -= ratio * _realisedGains[key];

        var scaledGains = _disposalGains.Select(g => new DisposalGainItem(g, 1.0 - ratio)).ToList(); 
        _disposalGains.Clear();
        _disposalGains.AddRange(scaledGains);
        
        var aggregatedScaledGains = _aggregatedDisposalGains.Select(g => new DisposalGainItem(g, 1.0 - ratio)).ToList(); 
        _aggregatedDisposalGains.Clear();
        _aggregatedDisposalGains.AddRange(aggregatedScaledGains);
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
        
        _sameDayDisposals.Disposals.Add(++_disposalSequence, time, quantity, cost);
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
           
            _aggregatedDisposalGains.Add(new DisposalGainItem()
            {
                Date = _sameDayDisposals.Date,
                TaxYear = _sameDayDisposals.TaxYear,
                Quantity = matched,
                CostBasis = matched*sameDayAcquisitionsAverageCost, 
                Proceeds = matched*sameDayDisposalsAverageCost,
                MatchType = DisposalMatchType.SameDay
            });
            _disposalGains.AddRange(
                _sameDayDisposals.GetDisposalGainItems(matched, sameDayAcquisitionsAverageCost, DisposalMatchType.SameDay));
            
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
            
            _aggregatedDisposalGains.Add(new DisposalGainItem()
            {
                Date = pool.Date,
                TaxYear = pool.TaxYear,
                Quantity = r,
                CostBasis = r*sameDayAcquisitionsAverageCost, 
                Proceeds = r*pool.AverageCost,
                MatchType = DisposalMatchType.BedAndBreakfast
            });
            _disposalGains.AddRange(
                pool.GetDisposalGainItems(r, sameDayAcquisitionsAverageCost, DisposalMatchType.BedAndBreakfast));
            
            remainingAcquisitions -= r;
            pool.Cost += r*pool.AverageCost;
            pool.Quantity += r;
        }

        var lastDisposalPool = _pendingDisposalsByAge.LastOrDefault();
        if (lastDisposalPool == null)
        {
            lastDisposalPool = new Pool();
            _sameDayDisposals.MoveTo(lastDisposalPool, remainingDisposals);
        }
        else
            lastDisposalPool = new Pool(lastDisposalPool);
        
        // age the pending disposals
        for(var i = _pendingDisposalsByAge.Count - 1; i > 0; i--)
        {
            var nextPool = _pendingDisposalsByAge[i];
            var pool = _pendingDisposalsByAge[i-1];
           
            pool.MoveTo(nextPool);
        }
        
        // move remaining same-day disposals to zero age pending pool
        if (_pendingDisposalsByAge.Count > 0)
            _sameDayDisposals.MoveTo(_pendingDisposalsByAge[0], remainingDisposals);
        
        // remaining disposals come from S104
        if (lastDisposalPool.Quantity < 0)
        {
            _realisedGains[lastDisposalPool.TaxYear] += -lastDisposalPool.Quantity*(lastDisposalPool.AverageCost - section104AverageCost);
            
            _aggregatedDisposalGains.Add(new DisposalGainItem()
            {
                Date = lastDisposalPool.Date,
                TaxYear = lastDisposalPool.TaxYear,
                Quantity = -lastDisposalPool.Quantity,
                CostBasis = -lastDisposalPool.Quantity*section104AverageCost, 
                Proceeds = -lastDisposalPool.Quantity*lastDisposalPool.AverageCost,
                MatchType = DisposalMatchType.Section104,
            });
            _disposalGains.AddRange(lastDisposalPool.GetDisposalGainItems(-lastDisposalPool.Quantity, section104AverageCost, DisposalMatchType.Section104));
            
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
        
        _sameDayDisposals.Clear();
        _sameDayAcquisitions.Clear();
    }

    private void ClosePendingPools()
    {
        var section104AverageCost = _section104Pool.AverageCost;

        if (_sameDayAcquisitions.Quantity != 0)
        {
            _section104Pool.Cost += _sameDayAcquisitions.Cost;
            _section104Pool.Quantity += _sameDayAcquisitions.Quantity;
       
            _sameDayAcquisitions.Clear();
        }

        if (_sameDayDisposals.Quantity != 0)
        {
            var quantity = -_sameDayDisposals.Quantity;
            var proceeds = quantity * _sameDayDisposals.AverageCost;
            var costBasis = quantity * section104AverageCost;
            
            _realisedGains[_sameDayDisposals.TaxYear] += proceeds - costBasis; 
            
            _aggregatedDisposalGains.Add(new DisposalGainItem()
            {
                Date = _sameDayDisposals.Date,
                TaxYear = _sameDayDisposals.TaxYear,
                Quantity = quantity, 
                CostBasis = costBasis, 
                Proceeds = proceeds, 
                MatchType = DisposalMatchType.Section104,
            });
            
            _disposalGains.AddRange(_sameDayDisposals.GetDisposalGainItems(quantity, section104AverageCost, DisposalMatchType.Section104));

            _section104Pool.Cost -= costBasis; 
            _section104Pool.Quantity -= quantity;
            _sameDayDisposals.Clear();
        }
        
        foreach (var pool in _pendingDisposalsByAge.Where(pool => pool.Quantity != 0))
        {
            var quantity = -pool.Quantity;
            var proceeds = quantity * pool.AverageCost;
            var costBasis = quantity * section104AverageCost;

            _realisedGains[pool.TaxYear] += proceeds - costBasis; // -pool.Quantity*(pool.AverageCost - section104AverageCost);
            
            _aggregatedDisposalGains.Add(new DisposalGainItem()
            {
                Date = pool.Date,
                TaxYear = pool.TaxYear,
                Quantity = quantity, 
                CostBasis = costBasis, 
                Proceeds = proceeds, 
                MatchType = DisposalMatchType.Section104,
            });
            _disposalGains.AddRange(pool.GetDisposalGainItems(quantity, section104AverageCost, DisposalMatchType.Section104));
            
            _section104Pool.Cost -= costBasis;// pool.Quantity*section104AverageCost;
            _section104Pool.Quantity -= quantity; //pool.Quantity;
            pool.Clear();
        }
    }

    private class Disposal(long sequence, DateTime date, double quantity, double proceeds)
    {
        public long Sequence => sequence;
        public DateTime Date => date;
        public double Quantity { get; set; } = quantity;
        public double Proceeds { get; set; } = proceeds;
        public double AverageProceeds => Quantity == 0 ? 0.0 : Proceeds / Quantity;
        
        public Disposal(long sequence, Time disposalTime, double quantity, double proceeds)
            : this(sequence, disposalTime.ToDateTime(), quantity, proceeds)
        { }

        public Disposal(Disposal other, double ratio = 1.0)
            : this(other.Sequence, other.Date, other.Quantity * ratio, other.Proceeds * ratio) 
        { }
    }

    private class Disposals : IEnumerable<Disposal>
    {
        private readonly List<Disposal> _items = [];

        public Disposals() { }

        private Disposals(IEnumerable<Disposal> items)
        {
            _items.AddRange(items.Select(x => new Disposal(x)));
        }
        
        public void Clear() => _items.Clear();
        
        public void Add(long sequence, Time disposalTime, double quantity, double proceeds)
        {
            if (quantity == 0)
                return;
            _items.Add(new Disposal(sequence, disposalTime, quantity, proceeds));
        }
        
        public void Add(IEnumerable<Disposal> other)
        {
            _items.AddRange(other.Select(x => new Disposal(x)));
        }

        public void ReplaceWith(Disposals other)
        {
            _items.Clear();
            _items.AddRange(other._items.Select(x => new Disposal(x)));
        }

        public void MoveTo(Disposals other)
        {
            other._items.Clear();
            other._items.AddRange(_items.Select(x => new Disposal(x)));
            _items.Clear();
        }

        private Disposals Scale(double ratio)
        {
            return new Disposals(_items.Select(x => new Disposal(x, ratio)));
        }
        
        public static Disposals operator *(Disposals disposals, double ratio)
        {
            return disposals.Scale(ratio);
        }

        public static Disposals operator *(double ratio, Disposals disposals)
        {
            return disposals.Scale(ratio);
        }        
        
        public IEnumerator<Disposal> GetEnumerator() => _items.Where(x => x.Quantity != 0).OrderBy(x => x.Sequence).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
    private record Pool
    {
        public Disposals Disposals { get; } = new();

        public double Quantity { get; set; }
        public double Cost { get; set; }
        public double AverageCost => Quantity == 0 ? 0.0 : Cost / Quantity;

        public DateTime Date { get; set; } = DateTime.MinValue;
        public int TaxYear => GetTaxYear(Date);

        public IEnumerable<DisposalGainItem> GetDisposalGainItems(double quantity, double costBasisPerUnit, DisposalMatchType matchType)
        {
            var disposalGains = new List<DisposalGainItem>();
            if (quantity == 0)
                return disposalGains;
            
            foreach (var lot in Disposals)
            {
                if (quantity <= 0)
                    break;

                var q = Math.Min(quantity, lot.Quantity);
                var proceeds = q * lot.AverageProceeds;
                
                var item = new DisposalGainItem()
                {
                    Date = lot.Date,
                    Quantity = q,
                    Proceeds = proceeds,
                    CostBasis = q*costBasisPerUnit,
                    MatchType = matchType,
                    TaxYear = GetTaxYear(lot.Date),
                };
                disposalGains.Add(item);

                lot.Quantity -= q;
                lot.Proceeds -= proceeds;
                quantity -= q;
            }
            
            return disposalGains;
        }

        public void Clear()
        {
            Quantity = 0;
            Cost = 0;
            Disposals.Clear();
        }
        
        public void MoveTo(Pool other)
        {
            other.Date = Date;
            other.Quantity = Quantity;
            other.Cost = Cost;
            Disposals.MoveTo(other.Disposals);
        }

        public void MoveTo(Pool other, double quantity)
        {
            var ratio = Quantity == 0 ? 0.0 : quantity / Quantity;

            other.Date = quantity != 0 ? Date : DateTime.MinValue;
            other.Quantity = quantity;
            other.Cost = Cost * ratio;
            other.Disposals.ReplaceWith(Disposals);
        }

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

            Disposals = new Disposals();
            Disposals.ReplaceWith(other.Disposals);
            Date = other.Date;
            Quantity = other.Quantity;
            Cost = other.Cost;
        }
    }
}
