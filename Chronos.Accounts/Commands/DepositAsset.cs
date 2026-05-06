using Chronos.Core;
using Newtonsoft.Json;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Commands;

/// <summary>
/// Represents a command to deposit an asset into an account.
/// </summary>
[method: JsonConstructor]
public class DepositAsset(string name, Quantity quantity) : Command
{
    /// <summary>
    /// Gets the account name associated with the deposit. 
    /// </summary>
    public string Name => name;

    /// <summary>
    /// Gets or sets the quantity of the asset being deposited.
    /// </summary>
    public Quantity Quantity => quantity;
    
    /// <inheritdoc/>
    public override string Target => name;
}