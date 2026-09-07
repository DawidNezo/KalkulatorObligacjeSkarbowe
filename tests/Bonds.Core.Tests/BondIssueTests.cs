using System.Globalization;
using Bonds.Core.Tests.Support;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class BondIssueTests
{
    [Theory]
    [InlineData(1, 12)]
    [InlineData(4, 3)]
    [InlineData(12, 1)]
    public void PeriodMonthsFollowFromThePeriodsPerYear(int periodsPerYear, int expectedMonths)
    {
        var issue = TestIssues.Coi() with { PeriodsPerYear = periodsPerYear };

        Assert.Equal(expectedMonths, issue.PeriodMonths);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(-1)]
    public void RejectsAPeriodLengthThatDoesNotDivideAYear(int periodsPerYear) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => (TestIssues.Coi() with { PeriodsPerYear = periodsPerYear }).Validated());

    [Fact]
    public void TermIsThePeriodCountTimesThePeriodLength()
    {
        Assert.Equal(3, TestIssues.Ots().TermMonths);
        Assert.Equal(12, TestIssues.Ror().TermMonths);
        Assert.Equal(144, TestIssues.Rod().TermMonths);
    }

    [Fact]
    public void ABondThatDoesNotCapitaliseIsABondThatPaysCoupons()
    {
        Assert.True(TestIssues.Ror().PaysCoupons);
        Assert.False(TestIssues.Edo().PaysCoupons);
    }

    [Fact]
    public void RejectsRoundingABaseThatDoesNotExist() =>
        Assert.Throws<ArgumentException>(
            () => (TestIssues.Coi() with { RoundCapitalizedBase = true }).Validated());

    [Fact]
    public void RejectsAFaceValueOfZero() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => (TestIssues.Coi() with { Nominal = Money.Zero }).Validated());

    [Fact]
    public void RejectsAPurchasePriceOfZero() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => (TestIssues.Coi() with { PurchasePrice = Money.Zero }).Validated());

    [Fact]
    public void RejectsAPurchasePriceBelowTheFaceValueBecauseTheDiscountTaxIsNotModelled()
    {
        var error = Assert.Throws<ArgumentException>(
            () => (TestIssues.Coi() with { PurchasePrice = Money.RoundToGrosz(99.90m) }).Validated());

        Assert.Contains("dyskonta", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsASaleWindowThatEndsBeforeItStarts() =>
        Assert.Throws<ArgumentException>(
            () => (TestIssues.Coi() with { SaleEnd = new DateOnly(2026, 7, 1) }).Validated());

    [Fact]
    public void RejectsAnEmptyCode() =>
        Assert.Throws<ArgumentException>(() => (TestIssues.Coi() with { Code = "  " }).Validated());

    [Fact]
    public void RejectsAPeriodCountOfZero() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => (TestIssues.Coi() with { PeriodCount = 0 }).Validated());

    [Theory]
    [InlineData("2026-07-31")]
    [InlineData("2026-09-01")]
    public void RejectsAPurchaseOutsideTheSaleWindow(string date) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TestIssues.Coi().EnsureCanBuyOn(DateOnly.Parse(date, CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData("2026-08-01")]
    [InlineData("2026-08-31")]
    public void AcceptsAPurchaseOnTheEdgeOfTheSaleWindow(string date) =>
        TestIssues.Coi().EnsureCanBuyOn(DateOnly.Parse(date, CultureInfo.InvariantCulture));
}
