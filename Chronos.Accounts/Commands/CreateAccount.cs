using Newtonsoft.Json;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Commands;

/// <summary>
/// Represents a command to create a new account with a specified name and account type.
/// </summary>
[method: JsonConstructor]
public class CreateAccount(string name, AccountType type) : Command, ICreateCommand
{
    /// <summary>
    /// Gets the name associated with the create account command.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// Gets the account type associated with the account.
    /// </summary>
    public AccountType Type => type;

    /// <inheritdoc/> 
    public override string Target => Name;
}