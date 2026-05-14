using Chronos.Accounts.Events;
using Chronos.Core;
using ZES.Infrastructure.Utils;
using ZES.Interfaces;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

public class AssetLedgerHandler : IProjectionHandler<AssetLedger>
{
    /// <inheritdoc/>
    public AssetLedger Handle(IEvent e, AssetLedger state)
    {
        return e switch
        {
            AssetTransactionStarted assetTransactionStarted => Handle(assetTransactionStarted, state),
            TransferStarted transferStarted => Handle(transferStarted, state),
            _ => state
        };
    }

    private AssetLedger Handle(AssetTransactionStarted e, AssetLedger state)
    {
        var newState = new AssetLedger(state);
        var account = e.AggregateRootId();
        newState.AddMovement(e.Asset.Denominator, e.Timestamp, account, e.Asset.Amount);
        if(e.Cost.Denominator.AssetType != AssetType.Currency)
            newState.AddMovement(e.Cost.Denominator, e.Timestamp, account, e.Cost.Amount);
        return newState;
    }

    private AssetLedger Handle(TransferStarted e, AssetLedger state)
    {
        var hasFeeDisposal = e.Fee != null && e.Fee.IsValid() && e.Fee.Amount != 0 && e.Fee.Denominator.AssetType != AssetType.Currency;

        if (e.Amount.Denominator.AssetType == AssetType.Currency && !hasFeeDisposal)
            return state;

        var newState = new AssetLedger(state);
        if(hasFeeDisposal)
            newState.AddMovement(e.Fee.Denominator, e.Timestamp, e.FromAccount, -e.Fee.Amount);
        return newState;
    }
}