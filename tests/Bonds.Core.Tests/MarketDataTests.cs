using Bonds.Core.Calendar;
using Bonds.Core.Market;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class MarketDataTests
{
    /// <summary>
    /// Ten, as in <see cref="Bonds.Core.Domain.NbpReferenceLinkedRate"/>. A literal
    /// here, thus a silent change of the production constant fails a test.
    /// </summary>
    private const int NbpReferenceLinkedRateOffset = 10;

    private static readonly DateOnly FirstOfSeptember = new(2026, 9, 1);

    [Fact]
    public void ReadsAPublishedReferenceRate()
    {
        var market = Build(knownThrough: new DateOnly(2026, 8, 31));

        var quote = market.NbpReferenceOn(FirstOfSeptember, NbpReferenceLinkedRateOffset);

        Assert.Equal(0.0450m, quote.Value);
        Assert.False(quote.IsProjected);
    }

    [Fact]
    public void FallsBackToTheAssumptionBeyondThePublishedData()
    {
        var market = Build(knownThrough: new DateOnly(2026, 1, 1));

        var quote = market.NbpReferenceOn(FirstOfSeptember, NbpReferenceLinkedRateOffset);

        Assert.Equal(0.0400m, quote.Value);
        Assert.True(quote.IsProjected);
    }

    [Fact]
    public void AStepFunctionHoldsTheLastValueUntilTheNextChange()
    {
        var series = new DatedRateSeries("nbp", new DateOnly(2026, 8, 31), new Dictionary<DateOnly, decimal>
        {
            [new DateOnly(2026, 1, 1)] = 0.0500m,
            [new DateOnly(2026, 6, 1)] = 0.0450m,
        });

        Assert.Equal(0.0500m, series.ValueOn(new DateOnly(2026, 5, 31)));
        Assert.Equal(0.0450m, series.ValueOn(new DateOnly(2026, 6, 1)));
        Assert.Equal(0.0450m, series.ValueOn(new DateOnly(2026, 8, 31)));
    }

    [Fact]
    public void AStepFunctionGivesNothingBeyondItsPublishedData()
    {
        var series = new DatedRateSeries("nbp", new DateOnly(2026, 8, 31), new Dictionary<DateOnly, decimal>
        {
            [new DateOnly(2026, 1, 1)] = 0.0500m,
        });

        Assert.Null(series.ValueOn(new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public void AStepFunctionRefusesADateBeforeItStarts()
    {
        var series = new DatedRateSeries("nbp", new DateOnly(2026, 8, 31), new Dictionary<DateOnly, decimal>
        {
            [new DateOnly(2026, 1, 1)] = 0.0500m,
        });

        Assert.Throws<ArgumentOutOfRangeException>(() => series.ValueOn(new DateOnly(2025, 12, 31)));
    }

    [Fact]
    public void AMonthlySeriesRefusesAGapInsideItsPublishedRange()
    {
        var series = new MonthlyRateSeries("cpi", new YearMonth(2026, 8), new Dictionary<YearMonth, decimal>
        {
            [new YearMonth(2026, 7)] = 0.0320m,
        });

        Assert.Throws<KeyNotFoundException>(() => series.ValueIn(new YearMonth(2026, 6)));
    }

    [Fact]
    public void AMonthlySeriesGivesNothingBeyondItsPublishedData()
    {
        var series = new MonthlyRateSeries("cpi", new YearMonth(2026, 8), new Dictionary<YearMonth, decimal>
        {
            [new YearMonth(2026, 8)] = 0.0320m,
        });

        Assert.Null(series.ValueIn(new YearMonth(2026, 9)));
    }

    [Fact]
    public void ASeriesWithNoDataPointIsRefused()
    {
        Assert.Throws<ArgumentException>(() =>
            new DatedRateSeries("nbp", new DateOnly(2026, 1, 1), new Dictionary<DateOnly, decimal>()));
        Assert.Throws<ArgumentException>(() =>
            new MonthlyRateSeries("cpi", new YearMonth(2026, 1), new Dictionary<YearMonth, decimal>()));
    }

    [Fact]
    public void ReadsTheRateInForceOnTheFixingDate()
    {
        // Ten business days before 1 September 2026 is 18 August 2026. A rate
        // change on 19 August must not reach the September period.
        var market = new MarketData(
            new DatedRateSeries("nbp", new DateOnly(2026, 8, 31), new Dictionary<DateOnly, decimal>
            {
                [new DateOnly(2025, 1, 1)] = 0.0450m,
                [new DateOnly(2026, 8, 19)] = 0.0500m,
            }),
            new MonthlyRateSeries("cpi", new YearMonth(2026, 7), new Dictionary<YearMonth, decimal>
            {
                [new YearMonth(2026, 7)] = 0.0320m,
            }),
            PolishBusinessDayCalendar.Instance,
            assumedNbpReference: 0.0400m,
            assumedCpi: 0.0350m);

        Assert.Equal(0.0450m, market.NbpReferenceOn(FirstOfSeptember, NbpReferenceLinkedRateOffset).Value);
    }

    private static MarketData Build(DateOnly knownThrough) =>
        new(
            new DatedRateSeries("nbp", knownThrough, new Dictionary<DateOnly, decimal>
            {
                [new DateOnly(2025, 1, 1)] = 0.0450m,
            }),
            new MonthlyRateSeries("cpi", new YearMonth(2026, 7), new Dictionary<YearMonth, decimal>
            {
                [new YearMonth(2026, 7)] = 0.0320m,
            }),
            PolishBusinessDayCalendar.Instance,
            assumedNbpReference: 0.0400m,
            assumedCpi: 0.0350m);
}
