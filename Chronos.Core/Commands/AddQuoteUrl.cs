using Newtonsoft.Json;

namespace Chronos.Core.Commands;

/// <summary>
/// Represents a command to add a URL for a quote associated with a specific asset or domain.
/// </summary>
[method: JsonConstructor]
public class AddQuoteUrl(string fordom, string url) : ZES.Infrastructure.Domain.Command
{
    /// <summary>
    /// Gets the URL associated with the quote. The URL provides additional information
    /// or reference material related to the specific asset or domain involved.
    /// </summary>
    public string Url => url;
    
    /// <inheritdoc/>
    public override string Target => fordom;
}
