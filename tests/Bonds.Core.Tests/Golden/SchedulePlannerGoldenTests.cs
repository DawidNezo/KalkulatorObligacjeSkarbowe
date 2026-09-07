using System.Globalization;
using Bonds.Core.Engine;
using Bonds.Core.Tests.Support;
using Xunit;

namespace Bonds.Core.Tests.Golden;

/// <summary>
/// Annex 3 of the ROR letter lists every interest period for every day of purchase
/// in August 2026. The rule that it shows is not obvious: the day of the month of
/// the purchase stays the anchor, and a month that is too short only shortens that
/// one boundary. A purchase on 31 August gives 30 September and then 31 October.
/// </summary>
public sealed class SchedulePlannerGoldenTests
{
    /// <summary>The end dates of the twelve periods, taken from annex 3 for a purchase on the 29th.</summary>
    private static readonly string[] EndsForDay29 =
    [
        "2026-09-29", "2026-10-29", "2026-11-29", "2026-12-29", "2027-01-29", "2027-02-28",
        "2027-03-29", "2027-04-29", "2027-05-29", "2027-06-29", "2027-07-29", "2027-08-29",
    ];

    /// <summary>The end dates for a purchase on the 30th.</summary>
    private static readonly string[] EndsForDay30 =
    [
        "2026-09-30", "2026-10-30", "2026-11-30", "2026-12-30", "2027-01-30", "2027-02-28",
        "2027-03-30", "2027-04-30", "2027-05-30", "2027-06-30", "2027-07-30", "2027-08-30",
    ];

    /// <summary>The end dates for a purchase on the 31st.</summary>
    private static readonly string[] EndsForDay31 =
    [
        "2026-09-30", "2026-10-31", "2026-11-30", "2026-12-31", "2027-01-31", "2027-02-28",
        "2027-03-31", "2027-04-30", "2027-05-31", "2027-06-30", "2027-07-31", "2027-08-31",
    ];

    [Theory]
    [InlineData(29)]
    [InlineData(30)]
    [InlineData(31)]
    public void MatchesAnnexThreeForTheShortMonthRows(int purchaseDay)
    {
        var expected = purchaseDay switch
        {
            29 => EndsForDay29,
            30 => EndsForDay30,
            _ => EndsForDay31,
        };

        var periods = SchedulePlanner.Build(TestIssues.Ror(), new DateOnly(2026, 8, purchaseDay));

        Assert.Equal(expected.Length, periods.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(DateOnly.Parse(expected[index], CultureInfo.InvariantCulture), periods[index].End);
        }
    }

    [Fact]
    public void EveryPeriodStartsWhereThePreviousOneEnds()
    {
        var periods = SchedulePlanner.Build(TestIssues.Ror(), new DateOnly(2026, 8, 31));

        for (var index = 1; index < periods.Length; index++)
        {
            Assert.Equal(periods[index - 1].End, periods[index].Start);
        }
    }

    [Fact]
    public void KeepsTheDayOfTheMonthForEveryOtherRowOfAnnexThree()
    {
        for (var purchaseDay = 1; purchaseDay <= 28; purchaseDay++)
        {
            var purchase = new DateOnly(2026, 8, purchaseDay);
            var periods = SchedulePlanner.Build(TestIssues.Ror(), purchase);

            foreach (var period in periods)
            {
                Assert.Equal(purchaseDay, period.End.Day);
            }
        }
    }

    [Theory]
    [InlineData(29, "2028-02-29")]
    [InlineData(30, "2028-02-29")]
    [InlineData(31, "2028-02-29")]
    [InlineData(28, "2028-02-28")]
    public void ClampsToTheLeapDayInALeapFebruary(int purchaseDay, string expectedEnd)
    {
        // A two-year bond bought in August 2026 crosses February 2028, which has
        // 29 days. The clamp must stop at the leap day and not at the 28th.
        var periods = SchedulePlanner.Build(TestIssues.Dor(), new DateOnly(2026, 8, purchaseDay));

        Assert.Equal(DateOnly.Parse(expectedEnd, CultureInfo.InvariantCulture), periods[17].End);
    }

    [Fact]
    public void ThreeMonthPeriodsClampTheSameWay()
    {
        // Paragraph 16 of the OTS letter: a bond bought on 31 August 2026 is
        // redeemed on 30 November 2026.
        var periods = SchedulePlanner.Build(TestIssues.Ots(), new DateOnly(2026, 8, 31));

        Assert.Single(periods);
        Assert.Equal(new DateOnly(2026, 11, 30), periods[0].End);
    }

    [Fact]
    public void ActualDaysCountTheFirstDayAndNotTheLast()
    {
        var periods = SchedulePlanner.Build(TestIssues.Ror(), new DateOnly(2026, 8, 1));

        Assert.Equal(31, periods[0].ActualDays);  // 1 August to 1 September
        Assert.Equal(30, periods[1].ActualDays);  // 1 September to 1 October
    }

    [Fact]
    public void RefusesAPurchaseOutsideTheSaleWindow() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SchedulePlanner.Build(TestIssues.Ror(), new DateOnly(2026, 9, 1)));
}
