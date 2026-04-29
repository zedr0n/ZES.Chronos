using Chronos.Accounts.Queries;

namespace Chronos.Accounts;

public class AssetPoolFactory
{
    public IAssetPools Create(int numberOfMatchingDays = 30)
    {
        return new UkAssetPools(numberOfMatchingDays);
    }
}