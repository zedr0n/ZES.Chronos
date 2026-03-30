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

/// <inheritdoc />
public class UpdateTicker<T>(string fordom) : UpdateTicker(fordom)
    where T : class, IJsonQuoteResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTicker{T}"/> class.
    /// </summary>
    /// <param name="command"><see cref="UpdateQuote"/> command</param>
    public UpdateTicker(UpdateTicker command)
        : this(command.Target)
    {
        CorrelationId = command.CorrelationId;
    }
}