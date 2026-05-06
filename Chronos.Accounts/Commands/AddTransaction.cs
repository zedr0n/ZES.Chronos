using Newtonsoft.Json;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Commands;

/// <summary>
/// Represents a command to add a transaction to a specific account.
/// </summary>
[method: JsonConstructor]
public class AddTransaction(string name, string txId) : Command
{
    /// <summary>
    /// Gets the name associated with the transaction.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// Gets the unique identifier of the transaction associated with the command.
    /// </summary>
    public string TxId => txId;

    /// <inheritdoc/>
    public override string Target => name;
}