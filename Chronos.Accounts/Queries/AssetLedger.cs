using System;
using System.Collections.Generic;
using System.Linq;
using Chronos.Core;
using NodaTime;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

public class AssetLedger : IState
{
    private readonly Dictionary<Asset, List<(Time timestamp, string account, double quantity)>> _assetMovements;

    public AssetLedger()
    {
        _assetMovements = new Dictionary<Asset, List<(Time timestamp, string account, double quantity)>>();
    }
    
    public AssetLedger(AssetLedger other)
    {
        _assetMovements = other._assetMovements.ToDictionary(pair => pair.Key, pair => new List<(Time ,string, double)>(pair.Value));
    }

    public void AddMovement(Asset asset, Time timestamp, string account, double quantity)
    {
        _assetMovements.TryAdd(asset, []);
        _assetMovements[asset].Add((timestamp, account, quantity));
    }
    
    public IEnumerable<string> GetAccountsWithAssets(IEnumerable<Asset> assets)
    {
        var movements = _assetMovements.Where(x => assets.Contains(x.Key)).SelectMany(x => x.Value);
        return movements.Select(x => x.account).Distinct();
    }
    
    public bool HasCrossAccountMatchingPair(string account, string otherAccount, Asset asset, Time upTo, int numberOfMatchingDays)
    {
        var disposalCutoff = upTo.ToInstant().EndOfDay().ToTime();
        var matchingCutoff = upTo.ToInstant().EndOfDay().Plus(Duration.FromDays(numberOfMatchingDays)).ToTime();
        var these = GetAccountAssetMovements(asset, account, matchingCutoff).ToList();
        var others = GetAccountAssetMovements(asset, otherAccount, matchingCutoff).ToList();
           
        return these
                   .Where(x => x.Time < disposalCutoff)
                   .Any(x => others.Any(y => WouldMatch(x, y, numberOfMatchingDays, upTo)))
               || others
                   .Where(y => y.Time < disposalCutoff)
                   .Any(y => these.Any(x => WouldMatch(y, x, numberOfMatchingDays, upTo)));
    }

    public bool HasCrossAccountMatchingPair(IEnumerable<string> accounts, string otherAccount, Asset asset, Time upTo, int numberOfMatchingDays)
    {
        return accounts.Any(account => HasCrossAccountMatchingPair(account, otherAccount, asset, upTo, numberOfMatchingDays));
    }

    public IReadOnlyList<CrossAccountAssetMatch> GetCrossAccountAssetMatches(Asset asset, Time upTo, int numberOfMatchingDays)
    {
        var disposalCutoff = upTo.ToInstant().EndOfDay().ToTime();
        var matchingCutoff = upTo.ToInstant().EndOfDay().Plus(Duration.FromDays(numberOfMatchingDays)).ToTime();
        var movements = _assetMovements
            .GetValueOrDefault(asset, [])
            .Where(x => x.timestamp < matchingCutoff)
            .Where(x => x.quantity != 0)
            .ToList();        
        
        var disposals = movements.Where(x => x.quantity < 0).Where(x => x.timestamp <= disposalCutoff).ToList();
        var acquisitions = movements.Where(x => x.quantity > 0).ToList();
        
        return disposals
            .SelectMany(disposal => acquisitions
                .Where(acquisition => acquisition.account != disposal.account)
                .Where(acquisition => WouldMatch(
                    (disposal.timestamp, disposal.quantity),
                    (acquisition.timestamp, acquisition.quantity),
                    numberOfMatchingDays,
                    upTo))
                .Select(acquisition => new CrossAccountAssetMatch(
                    disposal.account,
                    acquisition.account,
                    asset,
                    disposal.timestamp,
                    acquisition.timestamp,
                    -disposal.quantity,
                    acquisition.quantity)))
            .ToList();
    }
    
    public IEnumerable<(Time Time, double Amount)> GetAccountAssetMovements(
        Asset asset,
        string account,
        Time before)
    {
        var accountMovements = _assetMovements.GetValueOrDefault(asset, []).Where(x => x.account == account);
        return accountMovements
            .Where(x => x.timestamp < before)
            .Where(x => x.quantity != 0)
            .Select(x => (x.timestamp, x.quantity));
    }
   
    public sealed record CrossAccountAssetMatch(
        string DisposalAccount,
        string AcquisitionAccount,
        Asset Asset,
        Time DisposalTime,
        Time AcquisitionTime,
        double DisposalQuantity,
        double AcquisitionQuantity);    
    
    private static bool WouldMatch((Time Time, double Amount) disposal, (Time Time, double Amount) acquisition, int numberOfMatchingDays, Time transferDate)
    {
        if(disposal.Amount >= 0 || acquisition.Amount <= 0)
            return false;
            
        var match = acquisition.Time >= disposal.Time && acquisition.Time <= disposal.Time.ToInstant().Plus(Duration.FromDays(numberOfMatchingDays)).ToTime();
        match &= !(disposal.Time.StartOfDay() == transferDate.StartOfDay() && acquisition.Time.StartOfDay() == transferDate.StartOfDay());
        return match;
    }    
}