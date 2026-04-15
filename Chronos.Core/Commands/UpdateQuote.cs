using Chronos.Core.Net;
using Newtonsoft.Json;
using ZES.Interfaces.Net;

namespace Chronos.Core.Commands
{
    /// <summary>
    /// Represents a command used to update a quote for a specific asset pair.
    /// </summary>
    [method: JsonConstructor]
    public class UpdateQuote(string fordom) : ZES.Infrastructure.Domain.Command
    {
        /// <inheritdoc/>
        public override string Target => fordom;

        /// <summary>
        /// Gets or sets a value indicating whether the cache should be enforced during the execution of the update quote command.
        /// When set to true, cached data will be prioritised and utilized if available; otherwise, the update
        /// operation may fetch fresh data regardless of any existing cached entries.
        /// </summary>
        public bool EnforceCache { get; set; }
    }

    /// <inheritdoc />
    [method: JsonConstructor]
    public class UpdateQuote<T, TSearch>(string fordom) : UpdateQuote(fordom)
        where T : class, IWebQuoteJsonResult
        where TSearch : class, IWebSearchJsonResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateQuote{T, TSearch}"/> class.
        /// </summary>
        /// <param name="command">Base class command</param>
        public UpdateQuote(UpdateQuote command)
            : this(command.Target)
        {
            CorrelationId = command.CorrelationId;
            AncestorId = command.AncestorId;
            EnforceCache = command.EnforceCache;
            Ephemeral = command.Ephemeral;
            Timestamp = command.Timestamp;
        }
    }
}