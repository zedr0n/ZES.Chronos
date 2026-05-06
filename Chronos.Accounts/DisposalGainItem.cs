using System;
using Chronos.Core;
using ZES.Interfaces.Clocks;

namespace Chronos.Accounts;

public enum DisposalMatchType
{
    SameDay,
    BedAndBreakfast,
    Section104
}

public class DisposalGainItem
{
    public DateTime Date { get; init; }
    public DateTime? AcquisitionDate { get; init; }
    
    public double Gain => Proceeds - CostBasis;
    public double Quantity { get; init; }
    public double Proceeds { get; init; }
    public double CostBasis { get; init; }
    
    public int TaxYear { get; init; }
    public DisposalMatchType MatchType { get; init; }

    public DisposalGainItem() { }
    public DisposalGainItem(DisposalGainItem other, double ratio = 1.0)
    {
        Date = other.Date;
        AcquisitionDate = other.AcquisitionDate;
        Quantity = other.Quantity * ratio;
        Proceeds = other.Proceeds * ratio;
        CostBasis = other.CostBasis * ratio;
        TaxYear = other.TaxYear;
        MatchType = other.MatchType;
    }
    
    public override string ToString()
    {
        return $"Date: {Date}, AcquisitionDate: {AcquisitionDate}, Quantity: {Quantity}, Proceeds: {Proceeds}, CostBasis: {CostBasis}, Gain: {Gain}, TaxYear: {TaxYear}, MatchType: {MatchType}";
    }
}