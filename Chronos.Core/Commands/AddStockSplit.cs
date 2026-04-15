using Newtonsoft.Json;
using ZES.Infrastructure.Domain;

namespace Chronos.Core.Commands;

/// <summary>
/// Represents a command to add a stock split event for a specified asset pair.
/// </summary>
/// <remarks>
/// A stock split adjusts the total number of shares and their individual value.
/// This command includes the asset pair identifier and the split ratio to apply.
/// </remarks>
[method: JsonConstructor]
public class AddStockSplit(string fordom, double ratio) : Command
{
    /// <inheritdoc/>
    public override string Target => fordom;

    /// <summary>
    /// Gets the ratio representing the stock split adjustment.
    /// </summary>
    /// <remarks>
    /// The ratio indicates the proportion in which the shares are split or merged.
    /// For example, a ratio of 2.0 indicates a 2-for-1 stock split, doubling the
    /// number of shares while halving the value of each share.
    /// </remarks>
    public double Ratio => ratio;
}