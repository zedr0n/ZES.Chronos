using Chronos.Core.Commands;
using Chronos.Core.Events;
using ZES.Infrastructure.Domain;

namespace Chronos.Core.Sagas;

/// <summary>
/// Represents a saga for managing the lifecycle of an exchange operation, including handling asset pairs and updating tickers.
/// </summary>
/// <remarks>
/// The <see cref="ExchangeSaga"/> tracks the flow of state transitions throughout the lifecycle of an exchange operation.
/// It leverages state machine logic to respond to various triggers such as <see cref="State"/> and <see cref="Trigger"/>
/// and executes commands as required.
/// </remarks>
public class ExchangeSaga : StatelessSaga<ExchangeSaga.State, ExchangeSaga.Trigger>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeSaga"/> class.
    /// </summary>
    public ExchangeSaga()
    {
        RegisterWithParameters<AssetPairRegistered>(e => e.Fordom, Trigger.RegisterAssetPair);
        RegisterWithParameters<QuoteTickerAdded>(e => e.CorrelationId, Trigger.TickerUpdated);
    }

    /// <summary>
    /// Represents the triggers that initiate state transitions within the <see cref="ExchangeSaga"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="Trigger"/> enum defines the set of actions or events that can cause the
    /// <see cref="ExchangeSaga"/> to transition between its defined states. These triggers are used
    /// in conjunction with state machine configurations to handle operations such as asset pair
    /// registration and ticker updates.
    /// </remarks>
    public enum Trigger
    {
        RegisterAssetPair,
        TickerUpdated,
    }

    /// <summary>
    /// Represents the various states in the lifecycle of an exchange operation within the
    /// <see cref="ExchangeSaga"/>.
    /// </summary>
    /// <remarks>
    /// Each state corresponds to a phase in the processing of an exchange operation, such as
    /// initialization, querying data, or completing the operation. Transitions between states
    /// are controlled by triggers and domain events.
    /// </remarks>
    public enum State
    {
        Open,
        Querying,
        Completed,
    }
    
    /// <inheritdoc/>
    protected override void ConfigureStateMachine()
    {
        base.ConfigureStateMachine();
        
        var registerTrigger = GetTrigger<AssetPairRegistered>();
        
        StateMachine.Configure(State.Open)
            .Permit(Trigger.RegisterAssetPair, State.Querying);

        StateMachine.Configure(State.Querying)
            .Permit(Trigger.TickerUpdated, State.Completed)
            .OnEntryFrom(registerTrigger, Handle);
    }

    private void Handle(AssetPairRegistered e)
    {
        SendCommand(new UpdateTicker(e.Fordom));
    }
}