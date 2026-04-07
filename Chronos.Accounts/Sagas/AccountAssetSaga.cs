using Chronos.Accounts.Commands;
using Chronos.Accounts.Events;
using Chronos.Core;
using Chronos.Core.Commands;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;

namespace Chronos.Accounts.Sagas;

public class AccountAssetSaga : StatelessSaga<AccountAssetSaga.State, AccountAssetSaga.Trigger>
{
    public AccountAssetSaga()
    {
        RegisterWithParameters<AssetTransactionStarted>(e => e.CommandId.Id.ToString(), Trigger.TransactionRequested);
        RegisterWithParameters<AssetDeposited>(Trigger.AssetProcessed);
    }
    /// <summary>
    /// Triggers
    /// </summary>
    public enum Trigger
    {
        TransactionRequested,
        AssetProcessed,
    }

    /// <summary>
    /// States
    /// </summary>
    public enum State
    {
        Open, 
        Processing,
        Completed,
    }

    protected override void ConfigureStateMachine()
    {
        base.ConfigureStateMachine();
        
        var startTrigger = GetTrigger<AssetTransactionStarted>();
        
        StateMachine.Configure(State.Open)
            .Permit(Trigger.TransactionRequested, State.Processing);

        StateMachine.Configure(State.Processing)
            .OnEntryFrom(startTrigger, Handle)
            .Permit(Trigger.AssetProcessed, State.Completed);
    }

    private void Handle(AssetTransactionStarted e)
    {
        SendCommand(new RecordTransaction(Id, e.Cost, Transaction.TransactionType.Spend, $"{e.Asset.Denominator.AssetId} asset transaction"));
        SendCommand(new AddTransaction(e.AggregateRootId(), Id));
        SendCommand(new DepositAsset(e.AggregateRootId(), e.Asset));
    }
}