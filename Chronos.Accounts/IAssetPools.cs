using System.Collections.Generic;
using ZES.Interfaces.Clocks;

namespace Chronos.Accounts;

public interface IAssetPools
{
    double TotalQuantity { get; }
    double RealisedGain { get; }
    double CostBasis { get; }

    /// <summary>
    /// Retrieves a dictionary mapping tax years to the total realised gains for those years.
    /// </summary>
    /// <returns>A dictionary where the key represents the tax year and the value represents the realised gain for that year.</returns>
    public Dictionary<int, double> GetRealisedGainsPerTaxYear();

    public IReadOnlyList<DisposalGainItem> GetDisposalGains();
    
    void Acquire(Time time, double quantity, double cost);
    void Dispose(Time time, double quantity, double cost);
    void AdvanceTo(Time time);

    void TransferFrom(Time time, IAssetPools source, double quantity);
    void TransferOut(Time time, double quantity);
}