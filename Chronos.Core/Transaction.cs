using ZES.Infrastructure.Domain;

namespace Chronos.Core;

/// <summary>
/// Represents a financial transaction within the domain.
/// A transaction is an immutable aggregate that encapsulates a specific type of financial activity.
/// </summary>
public sealed class Transaction : AggregateRoot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Transaction"/> class.
    /// Represents a financial transaction in the system, serving as an aggregate root for transaction-related events.
    /// The <see cref="Transaction"/> class manages the lifecycle of a transaction, including its creation,
    /// details update, and association with quotes.
    /// </summary>
    /// <remarks>
    /// A Transaction is an immutable entity that is modified via domain events.
    /// It supports functionality to update transaction details and add quotes.
    /// Its state evolves through the application of events such as TransactionCreated,
    /// TransactionDetailsUpdated, and TransactionQuoteAdded.
    /// </remarks>
    public Transaction() 
    {
        Register<Events.TransactionCreated>(ApplyEvent); 
        Register<Events.TransactionDetailsUpdated>();
        Register<Events.TransactionQuoteAdded>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Transaction"/> class.
    /// Represents a financial transaction in the system, serving as an aggregate root for transaction-related domain events.
    /// </summary>
    /// <param name="txId">Unique identifier for the transaction</param>
    /// <param name="quantity">Quantity of the transaction</param>
    /// <param name="transactionType">Type of the transaction</param>
    /// <param name="comment">Optional comment associated with the transaction</param>
    /// <param name="assetId">Optional asset identifier associated with the transaction</param>
    /// <remarks>
    /// The <see cref="Transaction"/> class is designed to manage the lifecycle of a transaction, starting from its creation through domain events.
    /// It is an immutable entity that applies domain events, such as <see cref="Events.TransactionCreated"/>, to define its initial state and subsequent changes.
    /// Every transaction contains detailed information, including its unique ID, quantity, type, and an optional comment.
    /// The <see cref="TransactionType"/> enumeration provides categorization for the transaction, aiding in business logic and reporting.
    /// </remarks>
    public Transaction(string txId, Quantity quantity, TransactionType transactionType, string comment, string assetId = null)
        : this()
    {
        When(new Events.TransactionCreated(txId, quantity, transactionType, comment, assetId));
    }

    /// <summary>
    /// Represents the types of transactions within the Chronos.Core system.
    /// </summary>
    /// <remarks>
    /// TransactionType defines the nature or purpose of a transaction.
    /// It categorizes transactions to facilitate proper handling,
    /// reporting, or identification of specific kinds of activities.
    /// </remarks>
    public enum TransactionType
    {
        /// <summary>
        /// Represents a general type of transaction.
        /// This type is used for transactions that do not fall under
        /// any specialized category such as Asset, Fee, Dividend,
        /// Interest, Transfer, or Unknown.
        /// </summary>
        General,

        /// <summary>
        /// Represents a transaction type categorized as an <c>Asset</c>.
        /// This transaction type is usually associated with processes
        /// involving tangible or intangible assets, such as asset transfers,
        /// purchases, or modifications that impact the overall portfolio.
        /// </summary>
        Asset,

        /// <summary>
        /// Represents a transaction of type <c>Fee</c>, which is used to categorize transactions that incur associated costs or charges.
        /// This type is typically utilized to track expenses or deductions resulting from fees applied to an account or an asset.
        /// </summary>
        Fee,

        /// <summary>
        /// Represents a transaction type for dividend payouts.
        /// </summary>
        /// <remarks>
        /// The <c>Dividend</c> transaction type is used to record transactions that involve
        /// the distribution of earnings or profits, such as dividends issued to shareholders
        /// of a company. This type is commonly applicable in financial and investment contexts.
        /// </remarks>
        Dividend,

        /// <summary>
        /// Represents a transaction type where interest is applied or accrued.
        /// This may denote earnings or charges tied to the application of interest
        /// on an account or asset, such as savings account interest or loan interest.
        /// </summary>
        Interest,

        /// <summary>
        /// Represents a transaction type for transferring assets or funds between accounts or entities.
        /// This type is typically used to denote the movement of resources without altering the overall ownership or balance,
        /// such as transferring money between accounts or entities within the same domain.
        /// </summary>
        Transfer,

        /// <summary>
        /// Represents an unidentified or uncategorized type of transaction.
        /// This type is used when the nature or purpose of the transaction
        /// cannot be determined or does not match any predefined category.
        /// </summary>
        Unknown,
    }

    /// <summary>
    /// Updates the details of the transaction, including the transaction type and an optional comment.
    /// </summary>
    /// <param name="transactionType">The type of the transaction, determining its classification.</param>
    /// <param name="comment">An optional comment providing additional details about the transaction.</param>
    /// <remarks>
    /// This method triggers the application of a <see cref="Events.TransactionDetailsUpdated"/> event.
    /// It is used to modify transaction-specific information while maintaining the integrity of its event-sourcing design.
    /// </remarks>
    public void UpdateDetails (TransactionType transactionType, string comment)
    {
        When(new Events.TransactionDetailsUpdated(transactionType, comment));
    }

    /// <summary>
    /// Adds a quote to the current transaction.
    /// This method encapsulates the logic of appending additional quantity information
    /// to a transaction using domain events to ensure consistency and immutability
    /// of the transaction's state.
    /// </summary>
    /// <param name="quantity">
    /// The <see cref="Quantity"/> to be added as a quote to the transaction.
    /// This represents the amount and associated asset of the transaction.
    /// </param>
    /// <remarks>
    /// Adding a quote triggers the <see cref="Events.TransactionQuoteAdded"/> event,
    /// which records the addition of the specified quantity to the transaction.
    /// This allows for tracking all modifications related to transaction quotes.
    /// </remarks>
    public void AddQuote (Quantity quantity)
    {
        When(new Events.TransactionQuoteAdded(quantity));
    }  

    private void ApplyEvent (Events.TransactionCreated e)
    {
        Id = e.TxId;
    }  
}