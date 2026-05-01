using Chronos.Core;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Queries;

/// <summary>
/// Query for realised gains allocated to a tax year.
/// </summary>
/// <remarks>
/// When a timestamp is supplied, the handler evaluates account stats through 30 days after
/// that timestamp so UK 30-day matching can be applied to disposals near the reporting
/// boundary. Gains are still grouped by the disposal tax year.
/// </remarks>
public class RealisedGainsForTaxYearQuery(string account, int taxYear, Asset asset, Asset denominator) : Query<RealisedGainsForTaxYear>
{
    public string Account => account;
    public int TaxYear => taxYear;
    public Asset Asset => asset;
    public Asset Denominator => denominator;
    public bool QueryNet { get; set; }
}
