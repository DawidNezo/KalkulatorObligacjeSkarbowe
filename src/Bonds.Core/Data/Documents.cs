namespace Bonds.Core.Data;

/// <summary>
/// The shape of the JSON files. These types mirror the files and nothing else;
/// <see cref="JsonDataStore"/> turns them into domain objects and reports every
/// missing field with the name of its file.
/// </summary>
public sealed class IssueDocument
{
    public string? Code { get; set; }

    public string? Kind { get; set; }

    public string? Source { get; set; }

    public decimal? NominalZloty { get; set; }

    public decimal? PurchasePriceZloty { get; set; }

    public int? PeriodsPerYear { get; set; }

    public int? PeriodCount { get; set; }

    public bool? Capitalizes { get; set; }

    public bool? RoundCapitalizedBase { get; set; }

    public string? PrincipalFloor { get; set; }

    public SaleWindowDocument? Sale { get; set; }

    public RateDocument? Rate { get; set; }

    public FeeDocument? Fee { get; set; }

    public EarlyRedemptionDocument? EarlyRedemption { get; set; }
}

public sealed class SaleWindowDocument
{
    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }
}

public sealed class RateDocument
{
    /// <summary>One of "fixed", "nbpReference" or "inflation".</summary>
    public string? Type { get; set; }

    public decimal? FirstPeriodRatePercent { get; set; }

    public decimal? MarginPercent { get; set; }
}

public sealed class FeeDocument
{
    /// <summary>One of "twoTier", "accruedCapped" or "forfeitAllInterest".</summary>
    public string? Type { get; set; }

    public decimal? AmountZloty { get; set; }
}

public sealed class EarlyRedemptionDocument
{
    public int? MinDaysAfterPurchase { get; set; }

    public int? MinDaysBeforeMaturity { get; set; }
}

public sealed class ScenarioDocument
{
    public string? Name { get; set; }

    public decimal? AssumedCpiPercent { get; set; }

    public decimal? AssumedNbpReferencePercent { get; set; }
}

public sealed class DatedSeriesDocument
{
    public string? Name { get; set; }

    public DateOnly? KnownThrough { get; set; }

    public Dictionary<string, decimal>? Percent { get; set; }
}

public sealed class MonthlySeriesDocument
{
    public string? Name { get; set; }

    public string? KnownThrough { get; set; }

    public Dictionary<string, decimal>? Percent { get; set; }
}
