using System.Collections.Generic;
using Newtonsoft.Json;
using ZES.Infrastructure.Domain;

namespace Chronos.Core.Events;

/// <summary>
/// Represents an event signaling the addition of a future quote rule for a specific foreign/domestic pair.
/// </summary>
[method: JsonConstructor]
public class FutureQuoteRuleAdded(string fordom, string ruleType, Dictionary<string, object> parameters) : Event
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FutureQuoteRuleAdded"/> class.
    /// </summary>
    public FutureQuoteRuleAdded() 
        : this(null, null, null) { }
    
    /// <summary>
    /// Gets the identifier of the domain or entity associated with the future quote rule.
    /// </summary>
    public string Fordom => fordom;

    /// <summary>
    /// Gets the type of the rule associated with the future quote.
    /// </summary>
    public string RuleType => ruleType;

    /// <summary>
    /// Gets the collection of parameters associated with the future quote rule.
    /// </summary>
    /// <remarks>
    /// Parameters represent additional key-value information required to define the behavior of the rule.
    /// The keys are strings, while the values can be of any object type.
    /// </remarks>
    public Dictionary<string, object> Parameters => parameters;
}