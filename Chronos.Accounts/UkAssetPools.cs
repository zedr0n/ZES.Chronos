using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Clocks;

namespace Chronos.Accounts;

public class UkAssetPools : IAssetPools
{
    private readonly List<Pool> _pools;
    private readonly Pool _sameDayAcquisitions;
    private readonly Pool _sameDayDisposals;
    // pending disposals by age
    private readonly List<Pool> _pendingDisposalsByAge;
    private readonly Pool _section104Pool;
    
    // itemised disposal gains
    private readonly List<DisposalGainItem> _disposalGains = new();
    
    private DateTime _lastEndOfDay;
    private long _disposalSequence;
    private readonly bool _trackDisposalLots;
    
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
            return pools._disposalGains.Sum(g => g.Gain);
        }
    }
    
    /// <inheritdoc />
    public Dictionary<int, double> GetRealisedGainsPerTaxYear()
    {
        if (_lastEndOfDay == default)
            return new Dictionary<int, double>();
            
        var pools = new UkAssetPools(this);
        pools.EndOfDay(_lastEndOfDay.AddDays(_pendingDisposalsByAge.Count+1).ToTime());

        return pools._disposalGains.GroupBy(g => g.TaxYear).ToDictionary(g => g.Key, g => g.Sum(x => x.Gain)); 
    }

    public IReadOnlyList<DisposalGainItem> GetDisposalGains()
    {
        if (_lastEndOfDay == default)
            return []; 
            
        var pools = new UkAssetPools(this);
        pools.EndOfDay(_lastEndOfDay.AddDays(_pendingDisposalsByAge.Count+1).ToTime()); 
        return pools._disposalGains;
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

    private Pool CreatePool() => _trackDisposalLots ? new PoolWithLots() : new Pool();
    
    public UkAssetPools(int numberOfMatchingDays = 30, bool trackDisposalLots = false)
    {
        _trackDisposalLots = trackDisposalLots;
        ArgumentOutOfRangeException.ThrowIfNegative(numberOfMatchingDays);
        _sameDayDisposals = CreatePool();
        _sameDayAcquisitions = CreatePool();
        _section104Pool = CreatePool();

        _pendingDisposalsByAge = Enumerable.Range(0, numberOfMatchingDays).Select(_ => CreatePool()).ToList();
        _pools =
        [
            _sameDayAcquisitions,
            _sameDayDisposals
        ];
        _pools.AddRange(_pendingDisposalsByAge);
        _pools.Add(_section104Pool);
    }

    private UkAssetPools(UkAssetPools other)
    {
        _trackDisposalLots = other._trackDisposalLots;
        _lastEndOfDay = other._lastEndOfDay;
        _disposalSequence = other._disposalSequence;
        _sameDayAcquisitions = other._sameDayAcquisitions.Copy();
        _sameDayDisposals = other._sameDayDisposals.Copy();
        _section104Pool = other._section104Pool.Copy();
        _disposalGains = new List<DisposalGainItem>(other._disposalGains);
        
        _pendingDisposalsByAge = other._pendingDisposalsByAge.Select(p => p.Copy()).ToList();
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
            targetPool.Add(sourcePool, ratio);

        foreach (var v in s._disposalGains)
            _disposalGains.Add(new DisposalGainItem(v, ratio));
    }

    public void TransferOut(double quantity)
    {
        var effectivePosition = _pools.Sum(p => p.Quantity); 
        var ratio = effectivePosition > 0 ? quantity / effectivePosition : 0;

        foreach (var pool in _pools)
            pool.Scale(1.0-ratio);
        
        var scaledGains = _disposalGains.Select(g => new DisposalGainItem(g, 1.0 - ratio)).ToList(); 
        _disposalGains.Clear();
        _disposalGains.AddRange(scaledGains);
    }

    public void Acquire(Time time, double quantity, double cost)
    {
        _sameDayAcquisitions.Date = time.ToDateTime().Date;
        _sameDayAcquisitions.Add(quantity, cost);
    }

    public void Dispose(Time time, double quantity, double cost)
    {
        _sameDayDisposals.Date = time.ToDateTime().Date; 
        _sameDayDisposals.RecordDisposal(++_disposalSequence, time, quantity, cost);
    }

    public void EndOfDay(Time time)
    {
        var date = time.ToDateTime().Date;
        if (_lastEndOfDay == default)
            _lastEndOfDay = date.AddDays(-1);
        
        if (date == _lastEndOfDay) 
            return;

        
        // AccountStatsQueryHandler advances pools on every transaction date. If a later query skips
        // more than the matching window, no unprocessed acquisition can still match these
        // pending disposals, so they can be closed before jumping to the final ageing window.
        var matchingStartDate = date.AddDays(-_pendingDisposalsByAge.Count);
        if (_lastEndOfDay < matchingStartDate && _pendingDisposalsByAge.Count > 0)
            AdvanceToMatchingWindow(matchingStartDate);

        while (_lastEndOfDay < date)
            EndSingleDay();
    }
    
    private void EndSingleDay()
    {
        var processSameDayPools = _sameDayAcquisitions.Date == _lastEndOfDay 
                                  || _sameDayDisposals.Date == _lastEndOfDay;
        
        var remainingAcquisitions = processSameDayPools ? _sameDayAcquisitions.Quantity : 0;
        var remainingDisposals = processSameDayPools ? _sameDayDisposals.Quantity : 0;
        var sameDayDisposalsAverageCost = processSameDayPools ? _sameDayDisposals.AverageCost : 0;
        var sameDayAcquisitionsAverageCost = processSameDayPools ? _sameDayAcquisitions.AverageCost : 0;
        var section104AverageCost = _section104Pool.AverageCost;
        
        // same-day matched gain
        var matched = Math.Min(remainingAcquisitions, -remainingDisposals);
        if (matched > 0)
        {
            _disposalGains.AddRange(
                _sameDayDisposals.CreateDisposalGains(_sameDayDisposals.Date, matched, sameDayDisposalsAverageCost, sameDayAcquisitionsAverageCost, DisposalMatchType.SameDay, _sameDayAcquisitions.Date));
            
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
            
            _disposalGains.AddRange(
                pool.CreateDisposalGains(pool.Date, r, pool.AverageCost, sameDayAcquisitionsAverageCost, DisposalMatchType.BedAndBreakfast, _sameDayAcquisitions.Date));
            
            remainingAcquisitions -= r;
            pool.Add(r, r*pool.AverageCost);
        }

        var lastDisposalPool = _pendingDisposalsByAge.LastOrDefault();
        if (lastDisposalPool == null)
        {
            lastDisposalPool = new Pool();
            _sameDayDisposals.MoveTo(lastDisposalPool, remainingDisposals);
        }
        else
            lastDisposalPool = lastDisposalPool.Copy(); 
        
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
            _disposalGains.AddRange(
                lastDisposalPool.CreateDisposalGains(lastDisposalPool.Date, -lastDisposalPool.Quantity,  lastDisposalPool.AverageCost, section104AverageCost, DisposalMatchType.Section104));
            
            _section104Pool.Add(lastDisposalPool.Quantity, lastDisposalPool.Quantity*section104AverageCost);
        }
        
        // remaining acquisitions go to section 104
        if (remainingAcquisitions > 0)
            _section104Pool.Add(remainingAcquisitions, remainingAcquisitions*sameDayAcquisitionsAverageCost);

        _lastEndOfDay = _lastEndOfDay.AddDays(1);
        
        if (!processSameDayPools)
            return;
        
        _sameDayDisposals.Clear();
        _sameDayAcquisitions.Clear();
    }

    private void AdvanceToMatchingWindow(DateTime matchingStartDate)
    {
        EndSingleDay();
        if (_lastEndOfDay >= matchingStartDate)
            return;
        
        var section104AverageCost = _section104Pool.AverageCost;

        if (_sameDayAcquisitions.Quantity != 0)
        {
            _section104Pool.Add(_sameDayAcquisitions.Quantity, _sameDayAcquisitions.Cost);
            _sameDayAcquisitions.Clear();
        }

        if (_sameDayDisposals.Quantity != 0)
        {
            var quantity = -_sameDayDisposals.Quantity;
            var costBasis = quantity * section104AverageCost;
            
            _disposalGains.AddRange(
                _sameDayDisposals.CreateDisposalGains(_sameDayDisposals.Date, quantity, _sameDayDisposals.AverageCost, section104AverageCost, DisposalMatchType.Section104));

            _section104Pool.Add(-quantity, -costBasis);
            _sameDayDisposals.Clear();
        }
        
        foreach (var pool in _pendingDisposalsByAge.Where(pool => pool.Quantity != 0))
        {
            var quantity = -pool.Quantity;
            var costBasis = quantity * section104AverageCost;

            _disposalGains.AddRange(
                pool.CreateDisposalGains(pool.Date, quantity, pool.AverageCost, section104AverageCost, DisposalMatchType.Section104));
            
            _section104Pool.Add(-quantity, -costBasis);
            pool.Clear();
        }
        
        _lastEndOfDay = matchingStartDate;
    }

    private class Disposal(long sequence, DateTime date, double quantity)
    {
        public long Sequence => sequence;
        public DateTime Date => date;
        public double Quantity { get; set; } = quantity;

        public Disposal(long sequence, Time disposalTime, double quantity)
            : this(sequence, disposalTime.ToDateTime(), quantity)
        { }

        public Disposal(Disposal other, double ratio = 1.0)
            : this(other.Sequence, other.Date, other.Quantity * ratio) 
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
        
        public void Add(long sequence, Time disposalTime, double quantity)
        {
            if (quantity == 0)
                return;
            _items.Add(new Disposal(sequence, disposalTime, quantity));
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
    
    private class Pool
    {
        public double Quantity { get; private set; }
        public double Cost { get; private set; }
        public double AverageCost => Quantity == 0 ? 0.0 : Cost / Quantity;

        public DateTime Date { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Creates disposal gain records for a matched disposal quantity.
        /// </summary>
        /// <remarks>
        /// The base pool emits a single aggregated row. The realised gain represented by the row is
        /// <c>quantity * (proceedsPerUnit - costBasisPerUnit)</c>, exposed through <see cref="DisposalGainItem.Gain"/>.
        /// </remarks>
        /// <param name="date">The disposal date used for the aggregated row.</param>
        /// <param name="quantity">The matched disposal quantity.</param>
        /// <param name="proceedsPerUnit">The pooled proceeds per disposed unit.</param>
        /// <param name="costBasisPerUnit">The matched acquisition or section 104 cost basis per disposed unit.</param>
        /// <param name="matchType">The UK matching rule that produced this gain.</param>
        /// <param name="acquisitionDate">The acquisition date for same-day or bed-and-breakfast matches, when known.</param>
        /// <returns>One or more disposal gain records whose gains sum to the realised gain for the match.</returns>
        public virtual IEnumerable<DisposalGainItem> CreateDisposalGains(DateTime date, double quantity, double proceedsPerUnit, double costBasisPerUnit, DisposalMatchType matchType, DateTime? acquisitionDate = null)
        {
            return new List<DisposalGainItem>()
            {
                new()
                {
                    Date = date,
                    AcquisitionDate = acquisitionDate,
                    TaxYear = GetTaxYear(date),
                    Quantity = quantity,
                    CostBasis = quantity * costBasisPerUnit, 
                    Proceeds = quantity * proceedsPerUnit,
                    MatchType = matchType
                }
            };
        }

        public virtual void Add(Pool other, double ratio)
        {
            Quantity += other.Quantity * ratio;
            Cost += other.Cost * ratio;
            Date = other.Date;
        }

        public void Add(double quantity, double cost)
        {
            Quantity += quantity;
            Cost += cost;
        }
        
        public virtual void Scale(double ratio)
        {
            Quantity *= ratio;
            Cost *= ratio;
        }
        
        public virtual void RecordDisposal(long sequence, Time time, double quantity, double cost)
        {
            Quantity -= quantity;
            Cost -= cost;
        }
        
        public virtual void Clear()
        {
            Quantity = 0;
            Cost = 0;
        }
        
        public virtual void MoveTo(Pool other)
        {
            other.Date = Date;
            other.Quantity = Quantity;
            other.Cost = Cost;
        }

        public virtual void MoveTo(Pool other, double quantity)
        {
            var ratio = Quantity == 0 ? 0.0 : quantity / Quantity;

            other.Date = quantity != 0 ? Date : DateTime.MinValue;
            other.Quantity = quantity;
            other.Cost = Cost * ratio;
        }

        protected static int GetTaxYear(DateTime date)
        {
            if(date == DateTime.MinValue)
                return 0;
            
            var year = date.Year;
            if (date.Month < 4 || date is { Month: 4, Day: < 6 })
                return year - 1;
            return year;
        }
        
        public Pool Copy()
        {
            if(this is PoolWithLots poolWithLots)
                return new PoolWithLots(poolWithLots);
            return new Pool(this);
        }
        
        protected Pool(Pool other)
        {
            if (other == null)
                return;

            Date = other.Date;
            Quantity = other.Quantity;
            Cost = other.Cost;
        }
        
        public Pool() { }
    }

    private class PoolWithLots : Pool
    {
        private readonly Disposals _disposals = new();

        /// <inheritdoc />
        /// <remarks>
        /// This implementation preserves the same pooled per-unit proceeds and cost basis as the aggregated
        /// calculation, but splits the output across the recorded disposal lots in disposal sequence order.
        /// </remarks>
        public override IEnumerable<DisposalGainItem> CreateDisposalGains(DateTime date, double quantity, double proceedsPerUnit, double costBasisPerUnit, DisposalMatchType matchType, DateTime? acquisitionDate = null)
        {
            var disposalGains = new List<DisposalGainItem>();
            if (quantity == 0)
                return disposalGains;
            
            foreach (var lot in _disposals)
            {
                if (quantity <= 0)
                    break;

                var q = Math.Min(quantity, lot.Quantity);
                var proceeds = q * proceedsPerUnit; //lot.AverageProceeds;
                
                var item = new DisposalGainItem()
                {
                    Date = lot.Date,
                    AcquisitionDate = acquisitionDate,
                    Quantity = q,
                    Proceeds = proceeds,
                    CostBasis = q*costBasisPerUnit,
                    MatchType = matchType,
                    TaxYear = GetTaxYear(lot.Date),
                };
                disposalGains.Add(item);

                lot.Quantity -= q;
                quantity -= q;
            }
            
            return disposalGains;
        }
        
        
        public override void Add(Pool other, double ratio)
        {
            base.Add(other, ratio);
            if(other is PoolWithLots otherPoolWithLots)
                _disposals.Add(otherPoolWithLots._disposals * ratio);
        }
        
        public override void Scale(double ratio)
        {
            base.Scale(ratio);
            _disposals.ReplaceWith(_disposals * ratio);
        } 
        
        public override void RecordDisposal(long sequence, Time time, double quantity, double cost)
        {
            base.RecordDisposal(sequence, time, quantity, cost);
            _disposals.Add(sequence, time, quantity);
        }        
        
        public override void Clear()
        {
            base.Clear();
            _disposals.Clear();
        }
        
        public override void MoveTo(Pool other)
        {
            base.MoveTo(other);
            if(other is PoolWithLots otherPoolWithLots)
                _disposals.MoveTo(otherPoolWithLots._disposals);
        }
        
        public override void MoveTo(Pool other, double quantity)
        {
            base.MoveTo(other, quantity);
            if (other is PoolWithLots otherPoolWithLots)
                otherPoolWithLots._disposals.ReplaceWith(_disposals);
        }
        
        public PoolWithLots(PoolWithLots other)
            : base(other)
        {
            if(other == null)
                return;
            
            _disposals = new Disposals();
            _disposals.ReplaceWith(other._disposals);
        }
            
        public PoolWithLots() { }
    }
}
