using System.Globalization;
using Bonds.Core.Domain;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class InterestPeriodTests
{
    private static readonly InterestPeriod Period =
        new(0, new DateOnly(2026, 8, 17), new DateOnly(2026, 9, 17));

    [Fact]
    public void ActualDaysCountTheFirstDayAndNotTheLast() => Assert.Equal(31, Period.ActualDays);

    [Fact]
    public void NoDayHasPassedOnTheFirstDay() => Assert.Equal(0, Period.DaysAccruedTo(Period.Start));

    [Fact]
    public void TheWholePeriodHasPassedOnTheLastDay() =>
        Assert.Equal(Period.ActualDays, Period.DaysAccruedTo(Period.End));

    [Fact]
    public void CountsTheDaysInBetween() =>
        Assert.Equal(14, Period.DaysAccruedTo(new DateOnly(2026, 8, 31)));

    [Theory]
    [InlineData("2026-08-16")]
    [InlineData("2026-09-18")]
    public void RefusesADayOutsideThePeriod(string date) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Period.DaysAccruedTo(DateOnly.Parse(date, CultureInfo.InvariantCulture)));

    [Fact]
    public void ThePeriodSettlesItsLastDayAndNotItsFirst()
    {
        Assert.False(Period.Settles(Period.Start));
        Assert.True(Period.Settles(Period.End));
        Assert.True(Period.Settles(new DateOnly(2026, 9, 1)));
        Assert.False(Period.Settles(new DateOnly(2026, 9, 18)));
    }
}
