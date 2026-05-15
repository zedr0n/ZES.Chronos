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
    
    public bool HasCrossAccountMatchingPair(string account, string otherAccount, Asset asset, Time upTo, int numberOfMatchingDays)
    {
        var matchingCutoff = upTo.ToInstant().EndOfDay().Plus(Duration.FromDays(numberOfMatchingDays)).ToTime();
        var these = GetAccountAssetMovements(asset, account, matchingCutoff).ToList();
        var others = GetAccountAssetMovements(asset, otherAccount, matchingCutoff).ToList();
           
        return these.Any(x => others.Any(y => WouldMatch(x, y, numberOfMatchingDays, upTo) || WouldMatch(y, x, numberOfMatchingDays, upTo)));
    }

    public bool HasCrossAccountMatchingPair(IEnumerable<string> accounts, string otherAccount, Asset asset, Time upTo, int numberOfMatchingDays)
    {
        return accounts.Any(account => HasCrossAccountMatchingPair(account, otherAccount, asset, upTo, numberOfMatchingDays));
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
    
    private static bool WouldMatch((Time Time, double Amount) disposal, (Time Time, double Amount) acquisition, int numberOfMatchingDays, Time transferDate)
    {
        if(disposal.Amount >= 0 || acquisition.Amount <= 0)
            return false;
            
        var match = acquisition.Time >= disposal.Time && acquisition.Time <= disposal.Time.ToInstant().Plus(Duration.FromDays(numberOfMatchingDays)).ToTime();
        match &= !(disposal.Time.StartOfDay() == transferDate.StartOfDay() && acquisition.Time.StartOfDay() == transferDate.StartOfDay());
        return match;
    }    
}