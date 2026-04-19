using Chronos.Core.Events;

namespace Chronos.Core.Queries;

/// <summary>
/// Handles projection updates for transactions by processing various events and updating the TransactionInfo state accordingly.
/// </summary>
/// <remarks>
/// The TransactionInfoHandler is responsible for handling different types of events related to transactions,
/// such as TransactionCreated, TransactionDetailsUpdated, and TransactionQuoteAdded, and projecting these
/// changes onto the TransactionInfo object.
/// </remarks>
/// <example>
/// This handler resolves to specific implementations of the <c>Handle</c> method based on the event type being processed.
/// It uses dynamic dispatch to delegate event handling to the appropriate type-specific method.
/// </example>
public class TransactionInfoHandler : ZES.Interfaces.Domain.IProjectionHandler<TransactionInfo>
{
    /// <inheritdoc/>
    public TransactionInfo Handle (ZES.Interfaces.IEvent e, TransactionInfo state)
    {
        return Handle((dynamic)e, state);
    }

    /// <summary>
    /// Handles the event associated with a newly created transaction and updates the current transaction state.
    /// </summary>
    /// <param name="e">The event representing the creation of the transaction.</param>
    /// <param name="state">The current transaction state to be updated.</param>
    /// <returns>The updated transaction state containing details of the new transaction.</returns>
    public TransactionInfo Handle (Chronos.Core.Events.TransactionCreated e, TransactionInfo state)
    {
        return new TransactionInfo(e.TxId, e.Timestamp.ToInstant(), e.Quantity, e.TransactionType, e.Comment, e.AssetId);
    }

    /// <summary>
    /// Updates the transaction information state based on the provided event containing updated transaction details.
    /// </summary>
    /// <param name="e">The event containing the updated details of the transaction.</param>
    /// <param name="state">The current state of the transaction information.</param>
    /// <returns>The updated transaction information state incorporating the new details.</returns>
    public TransactionInfo Handle (Chronos.Core.Events.TransactionDetailsUpdated e, TransactionInfo state)
    {
        return new TransactionInfo(state.TxId, state.Date, state.Quantity, e.TransactionType, e.Comment, state.AssetId);
    }

    /// <summary>
    /// Handles the <see cref="TransactionQuoteAdded"/> event by updating the given transaction state with the added quote.
    /// </summary>
    /// <param name="e">The <see cref="TransactionQuoteAdded"/> event containing the details of the added quote.</param>
    /// <param name="state">The current state of the transaction before the event is applied.</param>
    /// <returns>A new <see cref="TransactionInfo"/> object that includes the updated state with the added quote.</returns>
    public TransactionInfo Handle (Chronos.Core.Events.TransactionQuoteAdded e, TransactionInfo state)
    {
        var newState = new TransactionInfo(state.TxId, state.Date, state.Quantity, state.TransactionType, state.Comment, state.AssetId);
        newState.Quotes.Add(e.Quantity);
        return newState;
    }
}