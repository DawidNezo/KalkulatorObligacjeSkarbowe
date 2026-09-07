using Bonds.Core.Domain;
using Bonds.Core.Engine;
using Bonds.Core.Tests.Support;
using Xunit;

namespace Bonds.Core.Tests;

/// <summary>
/// Rules that must hold for every issue, every day of purchase and every level of
/// inflation. The grid is fixed and not random, thus a failure always repeats.
/// </summary>
public sealed class RedemptionInvariantTests
{
    private static readonly int[] PurchaseDays = [1, 15, 28, 29, 30, 31];

    /// <summary>Deflation, zero, a normal level and a shock.</summary>
    private static readonly decimal[] IndexLevels = [-0.05m, 0m, 0.035m, 0.20m];

    private static readonly Money Nominal = Money.RoundToGrosz(100m);

    private readonly RedemptionCalculator calculator = new();

    [Fact]
    public void ThePaymentNeverFallsBelowTheFaceValueWhereTheFloorApplies()
    {
        ForEveryCase((issue, plan) =>
        {
            foreach (var day in SampleDays(plan))
            {
                var settlement = calculator.Settle(plan, day);
                var floorApplies = issue.Floor == PrincipalFloor.AllPeriods
                                   || (issue.Floor == PrincipalFloor.FirstPeriodOnly && settlement.PeriodIndex == 0);

                if (floorApplies)
                {
                    Assert.True(settlement.Payment >= Nominal,
                        $"{issue.Code} pays {settlement.Payment} on {day:yyyy-MM-dd}");
                }
            }
        });
    }

    [Fact]
    public void TheFeeNeverExceedsTheInterestEarnedWhereTheLetterCapsIt()
    {
        ForEveryCase((issue, plan) =>
        {
            if (issue.Fee is not AccruedCappedFee)
            {
                return;
            }

            foreach (var day in SampleDays(plan))
            {
                var settlement = calculator.Settle(plan, day);
                Assert.True(settlement.Fee <= settlement.Interest,
                    $"{issue.Code} takes {settlement.Fee} from {settlement.Interest} on {day:yyyy-MM-dd}");
            }
        });
    }

    [Fact]
    public void InterestNeverGoesNegative()
    {
        ForEveryCase((issue, plan) =>
        {
            foreach (var day in SampleDays(plan))
            {
                Assert.False(calculator.Settle(plan, day).Interest.IsNegative,
                    $"{issue.Code} shows negative interest on {day:yyyy-MM-dd}");
            }
        });
    }

    [Fact]
    public void TheValueOfACapitalisingBondNeverFallsAsTimePasses()
    {
        ForEveryCase((issue, plan) =>
        {
            // A bond that pays its coupons out drops back to the face value at every
            // period boundary, thus only a capitalising bond can grow without a break.
            if (!issue.Capitalizes)
            {
                return;
            }

            var days = SampleDays(plan).ToList();
            for (var index = 1; index < days.Count; index++)
            {
                var earlier = calculator.Settle(plan, days[index - 1]).BondValue;
                var later = calculator.Settle(plan, days[index]).BondValue;
                Assert.True(later >= earlier,
                    $"{issue.Code} falls from {earlier} to {later} between "
                    + $"{days[index - 1]:yyyy-MM-dd} and {days[index]:yyyy-MM-dd}");
            }
        });
    }

    [Fact]
    public void TheValueOnTheLastDayEqualsTheValueAtTheEndOfTheTerm()
    {
        ForEveryCase((issue, plan) =>
            Assert.Equal(calculator.MaturityValue(plan), calculator.Settle(plan, plan.Maturity).BondValue));
    }

    [Fact]
    public void AnEarlyRedemptionOfOtsAlwaysPaysExactlyTheFaceValue()
    {
        ForEveryCase((issue, plan) =>
        {
            if (issue.Kind != BondKind.Ots)
            {
                return;
            }

            foreach (var day in SampleDays(plan).Where(d => d != plan.Maturity))
            {
                Assert.Equal(Nominal, calculator.Settle(plan, day).Payment);
            }
        });
    }

    [Fact]
    public void DeflationLeavesTheMarginAndTheBondStillEarns()
    {
        var market = new FixedMarketData(nbpReference: -0.02m, cpi: -0.05m);

        foreach (var issue in TestIssues.All())
        {
            var plan = BondPlan.Create(issue, TestIssues.Purchase, market);
            Assert.True(calculator.MaturityValue(plan) >= Nominal, $"{issue.Code} loses face value on deflation");
        }
    }

    [Fact]
    public void ASharpIndexShockOverTwelveYearsStaysExact()
    {
        // A twelve-year bond at a CPI of 20 per cent compounds twelve times. The
        // amount must stay exact and must not overflow.
        var market = new FixedMarketData(nbpReference: 0.20m, cpi: 0.20m);
        var plan = BondPlan.Create(TestIssues.Rod(), TestIssues.Purchase, market);

        var expected = 1.0560m;
        for (var year = 2; year <= 12; year++)
        {
            expected *= 1.2250m;   // 20.00 CPI + 2.50 margin
        }

        Assert.Equal(Money.RoundToGrosz(100m * expected), calculator.MaturityValue(plan));
    }

    /// <summary>Every period boundary, plus a day inside each period.</summary>
    private static IEnumerable<DateOnly> SampleDays(BondPlan plan)
    {
        foreach (var period in plan.Periods)
        {
            var middle = period.Period.Start.AddDays(period.Period.ActualDays / 2);
            yield return middle;
            yield return period.Period.End;
        }
    }

    private static void ForEveryCase(Action<BondIssue, BondPlan> check)
    {
        foreach (var day in PurchaseDays)
        {
            var purchase = new DateOnly(2026, 8, day);
            foreach (var level in IndexLevels)
            {
                var market = new FixedMarketData(nbpReference: level, cpi: level);
                foreach (var issue in TestIssues.All())
                {
                    check(issue, BondPlan.Create(issue, purchase, market));
                }
            }
        }
    }
}
