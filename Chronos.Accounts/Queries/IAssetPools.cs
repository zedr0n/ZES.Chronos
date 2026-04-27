using ZES.Interfaces.Clocks;

namespace Chronos.Accounts.Queries;

public interface IAssetPools
{
    double TotalQuantity { get; }
    double RealisedGain { get; }
    double CostBasis { get; }
        
    void Acquire(Time time, double quantity, double cost);
    void Dispose(Time time, double quantity, double cost);
    void EndOfDay(Time time);

    void TransferFrom(IAssetPools source, double quantity);
    void TransferOut(double quantity);
}