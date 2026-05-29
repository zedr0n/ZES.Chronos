using System.Collections.Generic;
using Newtonsoft.Json;
using ZES.Infrastructure.Domain;

namespace Chronos.Core.Commands;

/// <summary>
/// Represents a command for adding a future quote rule to an asset pair.
/// </summary>
[method: JsonConstructor]
public class AddFutureQuoteRule(string fordom, string ruleType, Dictionary<string, object> parameters) : Command
{
    /// <summary>
    /// Gets the type of the rule being applied in the command. Represents a specific strategy or behavior
    /// associated with adding a future quote rule to an asset pair.
    /// </summary>
    public string RuleType => ruleType;

    /// <summary>
    /// Gets the dictionary of parameters required for configuring the future quote rule.
    /// These may include specific conditions or settings applicable to the rule.
    /// </summary>
    public Dictionary<string, object> Parameters => parameters;

    /// <summary>
    /// Gets the target asset pair for which the future quote rule is being added.
    /// </summary>
    public override string Target => fordom;
}