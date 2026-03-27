using ZES.Infrastructure.Domain;

namespace Chronos.Core.Commands;

/// <summary>
/// Represents a command to add a quote ticker for a specific target.
/// </summary>
public class AddQuoteTicker(string fordom, string ticker) : Command
{
    /// <inheritdoc/>
    public override string Target => fordom;

    /// <summary>
    /// Gets the ticker associated with the quote to be added.
    /// </summary>
    public string Ticker => ticker;
}