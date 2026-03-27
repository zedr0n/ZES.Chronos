using ZES.Infrastructure.Domain;

namespace Chronos.Core.Commands;

/// <summary>
/// Represents a command used to update the ticker associated with a specific domain.
/// </summary>
/// <remarks>
/// This command is targeted to a specific domain and ensures the ticker is updated accordingly.
/// </remarks>
public class UpdateTicker(string fordom) : Command
{
    /// <inheritdoc/>
    public override string Target => fordom;
}

/// <summary>
/// Represents a command used to update the ticker associated with a specific domain.
/// </summary>
/// <typeparam name="T">The type of the quote result.</typeparam>
/// <remarks>
/// Provides mechanisms to target a specific domain and update its ticker accordingly.
/// </remarks>
public class UpdateTicker<T>(string fordom) : UpdateTicker(fordom)
    where T : class, IJsonQuoteResult
{
    private readonly string _fordom = fordom;

    /// <inheritdoc/>
    public override string Target => _fordom;
}