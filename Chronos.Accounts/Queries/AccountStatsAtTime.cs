namespace Chronos.Accounts.Queries;

/// <summary>
/// Represents account statistics evaluated at a specific timestamp.
/// </summary>
public class AccountStatsAtTime
{
    /// <summary>
    /// Gets or sets the timestamp used to produce the account statistics.
    /// </summary>
    public string Date { get; set; }

    /// <summary>
    /// Gets or sets the account statistics for the timestamp.
    /// </summary>
    public AccountStats Stats { get; set; }
}
