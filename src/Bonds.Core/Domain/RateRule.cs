using Bonds.Core.Market;

namespace Bonds.Core.Domain;

/// <summary>
/// The rule that gives the yearly interest rate of one interest period. Each
/// letter gives the rate of the first period as a number, and a rule for the
/// periods after it.
/// </summary>
public abstract record RateRule
{
    /// <summary>The rate of the first interest period, as stated in the letter.</summary>
    public abstract decimal FirstPeriodRate { get; }

    /// <summary>
    /// Returns the rate of one interest period. <paramref name="periodIndex"/> is
    /// zero for the first period.
    /// </summary>
    public RateQuote RateFor(int periodIndex, DateOnly periodStart, IMarketData market)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(periodIndex);
        ArgumentNullException.ThrowIfNull(market);

        return periodIndex == 0
            ? new RateQuote(FirstPeriodRate, IsProjected: false)
            : RateForLaterPeriod(periodStart, market);
    }

    protected abstract RateQuote RateForLaterPeriod(DateOnly periodStart, IMarketData market);
}

/// <summary>A rate that does not change. OTS and TOS.</summary>
public sealed record FixedRate(decimal AnnualRate) : RateRule
{
    public override decimal FirstPeriodRate => AnnualRate;

    protected override RateQuote RateForLaterPeriod(DateOnly periodStart, IMarketData market) =>
        new(AnnualRate, IsProjected: false);
}

/// <summary>
/// The NBP reference rate plus a margin. The reference rate is the one in force
/// on the tenth business day before the first day of the calendar month in which
/// the interest period starts. A reference rate below zero counts as zero.
/// </summary>
public sealed record NbpReferenceLinkedRate(decimal FirstRate, decimal Margin) : RateRule
{
    /// <summary>The offset stated in the letters of ROR and DOR.</summary>
    public const int BusinessDaysBeforeMonth = 10;

    public override decimal FirstPeriodRate => FirstRate;

    protected override RateQuote RateForLaterPeriod(DateOnly periodStart, IMarketData market)
    {
        var firstOfMonth = new DateOnly(periodStart.Year, periodStart.Month, 1);
        var quote = market.NbpReferenceOn(firstOfMonth, BusinessDaysBeforeMonth);
        return quote with { Value = Math.Max(0m, quote.Value) + Margin };
    }
}

/// <summary>
/// The yearly CPI growth plus a fixed margin. The index is the one that the
/// President of Statistics Poland published in the month before the first month
/// of the interest period. An index below zero counts as zero, thus the rate
/// never falls below the margin.
/// </summary>
public sealed record InflationLinkedRate(decimal FirstRate, decimal Margin) : RateRule
{
    /// <summary>The lookback stated in the letters of COI, EDO, ROS and ROD.</summary>
    public const int PublicationMonthsBeforePeriod = 1;

    public override decimal FirstPeriodRate => FirstRate;

    protected override RateQuote RateForLaterPeriod(DateOnly periodStart, IMarketData market)
    {
        var publication = YearMonth.Of(periodStart).AddMonths(-PublicationMonthsBeforePeriod);
        var quote = market.CpiPublishedIn(publication);
        return quote with { Value = Math.Max(0m, quote.Value) + Margin };
    }
}
