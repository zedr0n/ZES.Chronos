using System.Collections.Generic;
using Chronos.Core.Net;
using Newtonsoft.Json;
using ZES.Infrastructure;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Net;

namespace Chronos.Core.Commands
{
    /// <summary>
    /// Represents a command used to update a quote for a specific asset pair.
    /// </summary>
    [method: JsonConstructor]
    public class UpdateQuote(string fordom) : ZES.Infrastructure.Domain.Command
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateQuote"/> class.
        /// </summary>
        /// <param name="command">Original command</param>
        public UpdateQuote(UpdateQuote command)
            : this(command.Target)
        {
            CorrelationId = command.CorrelationId;
            AncestorId = command.AncestorId;
            EnforceCache = command.EnforceCache;
            Ephemeral = command.Ephemeral;
            Timestamp = command.Timestamp;
            ForFallback = command.ForFallback;
            AdditionalTimestamps = command.AdditionalTimestamps;
            UseBatching = command.UseBatching;
        }
        
        /// <inheritdoc/>
        public override string Target => fordom;

        /// <summary>
        /// Gets or sets a value indicating whether the cache should be enforced during the execution of the update quote command.
        /// When set to true, cached data will be prioritised and utilized if available; otherwise, the update
        /// operation may fetch fresh data regardless of any existing cached entries.
        /// </summary>
        public bool EnforceCache { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the operation should serve as a fallback execution.
        /// </summary>
        /// <remarks>
        /// When set to <see langword="true"/>, the command is intended to act as a fallback mechanism,
        /// typically used in scenarios where the primary operation fails or is unavailable.
        /// Defaults to <see langword="false"/>.
        /// </remarks>
        public bool ForFallback { get; set; }

        /// <summary>
        /// Gets or sets the additional timestamps associated with the update quote operation.
        /// </summary>
        [JsonIgnore]
        public List<Time> AdditionalTimestamps { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether batching should be used during the processing of the command.
        /// </summary>
        [JsonIgnore] 
        public bool UseBatching { get; set; } = Configuration.UseBatching;
    }
}