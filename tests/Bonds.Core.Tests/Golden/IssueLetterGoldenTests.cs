using Bonds.Core.Engine;
using Bonds.Core.Market;
using Bonds.Core.Tests.Support;
using Xunit;

namespace Bonds.Core.Tests.Golden;

/// <summary>
/// The amounts that the issue letters of 21 July 2026 state, checked to the grosz.
/// These tests are the proof that the engine implements the letters and not a
/// guess about them.
/// </summary>
public sealed class IssueLetterGoldenTests
{
    private readonly RedemptionCalculator calculator = new();
    private readonly IMarketData market = new FixedMarketData(nbpReference: 0.0400m, cpi: 0.0350m);

    [Fact]
    public void OtsPaysFiftyGroszeOfInterest()
    {
        // Paragraph 14 of letter 73/2026: the interest due on the redemption day is 0.50 zloty.
        var plan = BondPlan.Create(TestIssues.Ots(), TestIssues.Purchase, market);

        Assert.Equal(Money.RoundToGrosz(0.50m), calculator.Coupon(plan, 0));
    }

    [Fact]
    public void OtsRedeemsAtHundredZlotyFiftyGrosze()
    {
        // Paragraph 17 of letter 73/2026: the claim from the redemption of one bond
        // is 100.50 zloty.
        var plan = BondPlan.Create(TestIssues.Ots(), TestIssues.Purchase, market);

        Assert.Equal(Money.RoundToGrosz(100.50m), calculator.MaturityValue(plan));
    }

    [Fact]
    public void OtsEarlyRedemptionPaysTheFaceValueAndNoInterest()
    {
        // Paragraph 23 of letter 73/2026: no interest is due, and the redemption
        // pays the face value.
        var plan = BondPlan.Create(TestIssues.Ots(), TestIssues.Purchase, market);
        var early = calculator.Settle(plan, TestIssues.Purchase.AddMonths(2));

        Assert.Equal(Money.RoundToGrosz(100m), early.Payment);
        Assert.Equal(early.Interest, early.Fee);
    }

    [Fact]
    public void TosFollowsAnnexOneOfLetter76()
    {
        // W = N * (1 + r)^3 with r = 4.40 per cent.
        var plan = BondPlan.Create(TestIssues.Tos(), TestIssues.Purchase, market);
        var expected = Money.RoundToGrosz(100m * 1.044m * 1.044m * 1.044m);

        Assert.Equal(expected, calculator.MaturityValue(plan));
        Assert.Equal(Money.RoundToGrosz(113.79m), calculator.MaturityValue(plan));
    }

    [Fact]
    public void TosCapitalisedBaseMatchesAnnexTwoOfLetter76()
    {
        // Annex 2 gives N1 = 100 * (1 + r) and N2 = 100 * (1 + r)^2, each rounded
        // to two decimals. The value at the end of a period must equal that base.
        var plan = BondPlan.Create(TestIssues.Tos(), TestIssues.Purchase, market);

        Assert.Equal(Money.RoundToGrosz(104.40m), calculator.Settle(plan, plan.Periods[0].Period.End).BondValue);
        Assert.Equal(Money.RoundToGrosz(108.99m), calculator.Settle(plan, plan.Periods[1].Period.End).BondValue);
    }

    [Fact]
    public void CoiCouponIsTheRateOnTheFaceValue()
    {
        // Annex 2 of letter 77/2026: O = N * r, with no day count for a whole period.
        var plan = BondPlan.Create(TestIssues.Coi(), TestIssues.Purchase, market);

        Assert.Equal(Money.RoundToGrosz(4.75m), calculator.Coupon(plan, 0));
        Assert.Equal(Money.RoundToGrosz(5.00m), calculator.Coupon(plan, 1));  // 3.50 + 1.50 margin
    }

    [Fact]
    public void MonthlyCouponIsTheYearlyRateOverTwelve()
    {
        // Annex 2 of letter 74/2026: O = N * r * a / (D * F). A whole period gives
        // a = D, thus the coupon is exactly N * r / 12 and never a day count over 365.
        var plan = BondPlan.Create(TestIssues.Ror(), TestIssues.Purchase, market);

        Assert.Equal(Money.RoundToGrosz(100m * 0.04m / 12m), calculator.Coupon(plan, 0));
        Assert.Equal(Money.RoundToGrosz(0.33m), calculator.Coupon(plan, 0));
    }

    [Fact]
    public void DorSecondMonthExitMatchesLetter75ToTheGrosz()
    {
        // Letter 75/2026: first rate 4.15 per cent, monthly coupon
        // 100 * 0.0415 / 12 = 0.3458... -> 0.35 zloty. An exit at the end of the
        // second period pays the coupon of the first period, and the redemption
        // pays 100.35 minus the full fee of 0.70 zloty; the floor of the letter
        // covers the first period only, thus the payment is 99.65 zloty.
        var plan = BondPlan.Create(TestIssues.Dor(), TestIssues.Purchase, market);
        var exit = calculator.Settle(plan, TestIssues.Purchase.AddMonths(2));

        Assert.Equal(Money.RoundToGrosz(0.35m), calculator.Coupon(plan, 0));
        Assert.Equal(Money.RoundToGrosz(0.70m), exit.Fee);
        Assert.Equal(Money.RoundToGrosz(99.65m), exit.Payment);
    }

    [Fact]
    public void EdoFollowsAnnexTwoOfLetter78()
    {
        // W = N * (1+r1) * ... * (1+r10), with no rounding in between.
        var plan = BondPlan.Create(TestIssues.Edo(), TestIssues.Purchase, market);

        var product = 1.0535m;
        for (var year = 2; year <= 10; year++)
        {
            product *= 1.0550m;  // 3.50 CPI + 2.00 margin
        }

        Assert.Equal(Money.RoundToGrosz(100m * product), calculator.MaturityValue(plan));
    }

    [Fact]
    public void RosAndRodFollowTheSameProductFormula()
    {
        foreach (var (issue, periods, firstRate, laterRate) in new[]
                 {
                     (TestIssues.Ros(), 6, 1.0500m, 1.0550m),
                     (TestIssues.Rod(), 12, 1.0560m, 1.0600m),
                 })
        {
            var plan = BondPlan.Create(issue, TestIssues.Purchase, market);
            var product = firstRate;
            for (var year = 2; year <= periods; year++)
            {
                product *= laterRate;
            }

            Assert.Equal(Money.RoundToGrosz(100m * product), calculator.MaturityValue(plan));
        }
    }

    [Fact]
    public void PartialPeriodUsesTheRealDayCountOfThatPeriod()
    {
        // WP = N * (1 + r * a / (ACT * F)). For COI the period is one year, thus
        // ACT is 365 or 366 and F is one.
        var plan = BondPlan.Create(TestIssues.Coi(), TestIssues.Purchase, market);
        var period = plan.Periods[0].Period;
        var day = TestIssues.Purchase.AddMonths(6);
        var accruedDays = period.DaysAccruedTo(day);

        var expected = Money.RoundToGrosz(100m * (1m + (0.0475m * accruedDays / period.ActualDays)));

        Assert.Equal(expected, calculator.Settle(plan, day).BondValue);
    }
}
