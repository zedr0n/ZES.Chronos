namespace Chronos.Accounts;

public class AssetPoolFactory
{
    public IAssetPools Create(int numberOfMatchingDays = 30, bool trackDisposalLots = false)
    {
        return new UkAssetPools(numberOfMatchingDays, trackDisposalLots);
    }
}