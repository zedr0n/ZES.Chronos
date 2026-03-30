namespace Chronos.Core.Commands
{
    /// <summary>
    /// Represents a command used to update a quote for a specific asset pair.
    /// </summary>
    public class UpdateQuote(string fordom) : ZES.Infrastructure.Domain.Command
    {
        /// <inheritdoc/>
        public override string Target => fordom;
    }

    /// <inheritdoc />
    public class UpdateQuote<T>(string fordom) : UpdateQuote(fordom)
        where T : class, IJsonQuoteResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateQuote{T}"/> class.
        /// </summary>
        /// <param name="command">Base class command</param>
        public UpdateQuote(UpdateQuote command)
            : this(command.Target)
        {
            CorrelationId = command.CorrelationId;
        }
    }
}