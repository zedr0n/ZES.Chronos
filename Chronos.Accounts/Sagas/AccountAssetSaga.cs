using Chronos.Accounts.Commands;
using Chronos.Accounts.Events;
using Chronos.Core;
using Chronos.Core.Commands;
using Chronos.Core.Events;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;

namespace Chronos.Accounts.Sagas;

public class AccountAssetSaga : StatelessSaga<AccountAssetSaga.State, AccountAssetSaga.Trigger>
{
    private Quantity _quantity;
    private Quantity _cost;
    private Quantity _fee;
    private string _account;
    
    public AccountAssetSaga()
    {
        RegisterWithParameters<AssetTransactionStarted>(e => e.CommandId.Id.ToString(), Trigger.TransactionRequested);
        RegisterWithParameters<QuoteAdded>(e => e.CorrelationId, Trigger.QuoteAdded);
        RegisterWithParameters<AssetDeposited>(Trigger.AssetProcessed);
    }
    /// <summary>
    /// Triggers
    /// </summary>
    public enum Trigger
    {
        TransactionRequested,
        QuoteAdded,
        AssetProcessed,
    }

    /// <summary>
    /// States
    /// </summary>
    public enum State
    {
        Open, 
        Processing,
        CostUpdated,
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
            .Permit(Trigger.QuoteAdded, State.CostUpdated)
            .Permit(Trigger.AssetProcessed, State.Completed);

        StateMachine.Configure(State.CostUpdated)
            .OnEntryFrom(GetTrigger<QuoteAdded>(), Handle)
            .Permit(Trigger.AssetProcessed, State.Completed);
    }

    private void Handle(AssetTransactionStarted e)
    {
        _quantity = e.Asset;
        _cost = e.Cost;
        _fee = e.Fee;
        _account = e.AggregateRootId();
        var queryQuote = !_cost.IsValid();

        if (queryQuote)
        {
            var domAsset = e.Cost.Denominator;
            if (domAsset.IsValid())
            {
                SendCommand(new UpdateQuote(AssetPair.Fordom(e.Asset.Denominator, domAsset))
                    { EnforceCache = true });
            }
        }
        else
            DoTransaction();
    }

    private void Handle(QuoteAdded e)
    {
        var price = e.Close;
        _cost = _cost with { Amount = price * _quantity.Amount };
        DoTransaction();
    }

    private void DoTransaction()
    {
        SendCommand(new CreateTransaction(Id, _cost with { Amount = -_cost.Amount }, Transaction.TransactionType.Asset, $"{_quantity.Denominator.AssetId} asset transaction", _quantity.Denominator.AssetId));
        SendCommand(new AddTransaction(_account, Id));
        if (_fee != null && _fee.IsValid() && _fee.Amount != 0)
        {
            SendCommand(new CreateTransaction($"Fee_{Id}", _fee with { Amount = -_fee.Amount }, Transaction.TransactionType.Fee,
                $"{_quantity.Denominator.AssetId} asset transaction fee", _quantity.Denominator.AssetId));
            SendCommand(new AddTransaction(_account, $"Fee_{Id}"));
        }

        SendCommand(new DepositAsset(_account, _quantity));
    }
}