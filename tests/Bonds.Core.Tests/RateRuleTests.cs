using Bonds.Core.Domain;
using Bonds.Core.Tests.Support;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class RateRuleTests
{
    private static readonly DateOnly PeriodStart = new(2026, 9, 17);

    [Fact]
    public void FirstPeriodAlwaysUsesTheRateFromTheLetter()
    {
        var market = new FixedMarketData(nbpReference: 0.9000m, cpi: 0.9000m);

        Assert.Equal(0.0400m, new NbpReferenceLinkedRate(0.0400m, 0.0015m)
            .RateFor(0, PeriodStart, market).Value);
        Assert.Equal(0.0535m, new InflationLinkedRate(0.0535m, 0.0200m)
            .RateFor(0, PeriodStart, market).Value);
    }

    [Fact]
    public void NbpRuleAddsTheMargin()
    {
        var market = new FixedMarketData(nbpReference: 0.0400m, cpi: 0m);

        Assert.Equal(0.0415m, new NbpReferenceLinkedRate(0.0400m, 0.0015m)
            .RateFor(1, PeriodStart, market).Value);
    }

    [Fact]
    public void NbpRuleTreatsARateBelowZeroAsZero()
    {
        var market = new FixedMarketData(nbpReference: -0.0100m, cpi: 0m);

        Assert.Equal(0.0015m, new NbpReferenceLinkedRate(0.0400m, 0.0015m)
            .RateFor(1, PeriodStart, market).Value);
    }

    [Fact]
    public void NbpRuleAsksForTheTenthBusinessDayBeforeTheMonthOfThePeriod()
    {
        var market = new FixedMarketData(nbpReference: 0.0400m, cpi: 0m);

        new NbpReferenceLinkedRate(0.0400m, 0m).RateFor(1, PeriodStart, market);

        // The period starts on 17 September, thus the rule asks about the month
        // boundary of 1 September. The date arithmetic itself belongs to the
        // calendar and is tested with it.
        Assert.Equal((new DateOnly(2026, 9, 1), 10), market.LastNbpRequest);
    }

    [Fact]
    public void InflationRuleAddsTheMargin()
    {
        var market = new FixedMarketData(nbpReference: 0m, cpi: 0.0350m);

        Assert.Equal(0.0550m, new InflationLinkedRate(0.0535m, 0.0200m)
            .RateFor(1, PeriodStart, market).Value);
    }

    [Fact]
    public void DeflationLeavesTheMarginAndNotZero()
    {
        var market = new FixedMarketData(nbpReference: 0m, cpi: -0.0200m);

        // The letters put the floor on the index and not on the rate, thus the rate
        // falls to the margin and no lower.
        Assert.Equal(0.0200m, new InflationLinkedRate(0.0535m, 0.0200m)
            .RateFor(1, PeriodStart, market).Value);
    }

    [Fact]
    public void InflationRuleReadsTheMonthBeforeThePeriod()
    {
        var market = new FixedMarketData(nbpReference: 0m, cpi: 0.0350m);

        new InflationLinkedRate(0.0535m, 0.0200m).RateFor(1, PeriodStart, market);

        Assert.Equal(new YearMonth(2026, 8), market.LastCpiPublicationMonth);
    }

    [Fact]
    public void FixedRuleIgnoresTheMarket()
    {
        var market = new FixedMarketData(nbpReference: 0.9000m, cpi: 0.9000m);

        Assert.Equal(0.0440m, new FixedRate(0.0440m).RateFor(5, PeriodStart, market).Value);
    }

    [Fact]
    public void MarksAProjectedRate()
    {
        var market = new FixedMarketData(nbpReference: 0.0400m, cpi: 0.0350m, projected: true);

        Assert.True(new InflationLinkedRate(0.0535m, 0.0200m).RateFor(1, PeriodStart, market).IsProjected);
        Assert.False(new InflationLinkedRate(0.0535m, 0.0200m).RateFor(0, PeriodStart, market).IsProjected);
    }
}
