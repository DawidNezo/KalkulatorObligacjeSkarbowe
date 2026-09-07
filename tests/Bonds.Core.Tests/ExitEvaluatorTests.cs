using System.Collections.Immutable;
using Bonds.Core.Domain;
using Bonds.Core.Engine;
using Bonds.Core.Market;
using Bonds.Core.Tests.Support;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class ExitEvaluatorTests
{
    private readonly IMarketData market = new FixedMarketData(nbpReference: 0.0400m, cpi: 0.0350m);
    private readonly ExitEvaluator evaluator =
        new(new RedemptionCalculator(), new TaxCalculator(TaxCalculator.StandardRate));

    [Fact]
    public void TheTableRunsOneMonthPastTheEndOfTheTerm()
    {
        var rows = Evaluate(TestIssues.Ror());

        Assert.Equal(13, rows.Length);
        Assert.Equal(ExitKind.Maturity, rows[11].Kind);
        Assert.Equal(ExitKind.DoesNotExist, rows[12].Kind);
        Assert.Null(rows[12].Details);
    }

    [Fact]
    public void ARowThatDoesNotExistCarriesNoFigures()
    {
        var rows = Evaluate(TestIssues.Ots());

        var beyond = rows[^1];
        Assert.Equal(4, beyond.MonthIndex);
        Assert.Equal(ExitKind.DoesNotExist, beyond.Kind);
        Assert.Null(beyond.Details);
    }

    [Fact]
    public void TheRedemptionAtTheEndOfTheTermCarriesNoFee()
    {
        var rows = Evaluate(TestIssues.Edo());
        var maturity = rows.Single(r => r.Kind == ExitKind.Maturity);

        Assert.NotNull(maturity.Details);
        Assert.Equal(Money.Zero, maturity.Details.Fee);
        Assert.Equal(120, maturity.MonthIndex);
    }

    [Fact]
    public void EveryEarlierMonthIsAnEarlyRedemption()
    {
        var rows = Evaluate(TestIssues.Coi());

        Assert.All(rows.Where(r => r.MonthIndex < 48), r => Assert.Equal(ExitKind.EarlyRedemption, r.Kind));
    }

    [Fact]
    public void CouponsAddUpAsThePeriodsPass()
    {
        var rows = Evaluate(TestIssues.Ror());

        // A monthly coupon of 0.33 zloty: after five months four coupons are paid,
        // because the fifth one sits inside the redemption payment.
        Assert.Equal(Money.RoundToGrosz(1.32m), rows[4].Details!.CouponsGross);
        Assert.Equal(Money.RoundToGrosz(0.33m), rows[4].Details!.RedemptionInterestGross);
    }

    [Fact]
    public void ACapitalisingBondPaysNoCouponAtAll()
    {
        var rows = Evaluate(TestIssues.Edo());

        Assert.All(rows.Where(r => r.Details is not null),
            r => Assert.Equal(Money.Zero, r.Details!.CouponsGross));
    }

    [Fact]
    public void TheFeeLowersTheTaxableInterest()
    {
        var rows = Evaluate(TestIssues.Edo());
        var details = rows[12].Details!;   // month 13, second interest period

        var expectedBase = details.RedemptionInterestGross - details.Fee;
        var expectedTax = Money.CeilToGrosz(expectedBase * TaxCalculator.StandardRate);

        Assert.Equal(expectedTax, details.Settlement.RedemptionTax);
    }

    [Fact]
    public void AFeeThatSwallowsTheInterestLeavesNoTax()
    {
        var rows = Evaluate(TestIssues.Edo());
        var firstMonth = rows[0].Details!;

        Assert.Equal(firstMonth.RedemptionInterestGross, firstMonth.Fee);
        Assert.Equal(Money.Zero, firstMonth.Settlement.TotalTax);
        Assert.Equal(Money.Zero, firstMonth.Settlement.NetProfit);
    }

    [Fact]
    public void TheFloorHoldsTheFirstPeriodOfAnIssueThatPaysCoupons()
    {
        var rows = Evaluate(TestIssues.Ror());

        Assert.Equal(Money.RoundToGrosz(100m), rows[0].Details!.RedemptionGross);
    }

    [Fact]
    public void APaymentCanFallBelowTheFaceValueFromTheSecondPeriod()
    {
        // The floor of the DOR letter covers the first interest period only, thus
        // an exit one month later pays 100 + 0.35 - 0.70 = 99.65 zloty.
        var rows = Evaluate(TestIssues.Dor());
        var secondMonth = rows[1].Details!;

        Assert.True(secondMonth.RedemptionGross < Money.RoundToGrosz(100m));
        Assert.True(secondMonth.Settlement.NetProfit.IsNegative);
    }

    [Fact]
    public void ACapitalisingBondNeverPaysBelowTheFaceValue()
    {
        foreach (var issue in new[] { TestIssues.Tos(), TestIssues.Ros(), TestIssues.Edo(), TestIssues.Rod() })
        {
            foreach (var row in Evaluate(issue).Where(r => r.Details is not null))
            {
                Assert.True(row.Details!.RedemptionGross >= Money.RoundToGrosz(100m),
                    $"{issue.Code} month {row.MonthIndex} pays {row.Details.RedemptionGross}");
            }
        }
    }

    [Fact]
    public void TheValueOfACapitalisingBondNeverFalls()
    {
        foreach (var issue in new[] { TestIssues.Tos(), TestIssues.Ros(), TestIssues.Edo(), TestIssues.Rod() })
        {
            var rows = Evaluate(issue).Where(r => r.Details is not null).ToList();
            for (var index = 1; index < rows.Count; index++)
            {
                Assert.True(rows[index].Details!.RedemptionGross >= rows[index - 1].Details!.RedemptionGross,
                    $"{issue.Code} falls between month {rows[index - 1].MonthIndex} and {rows[index].MonthIndex}");
            }
        }
    }

    [Fact]
    public void NetProfitNeverExceedsGrossProfit()
    {
        foreach (var issue in TestIssues.All())
        {
            foreach (var row in Evaluate(issue).Where(r => r.Details is not null))
            {
                var details = row.Details!;
                Assert.True(details.Settlement.NetProfit <= details.TotalInterestGross);
            }
        }
    }

    [Fact]
    public void TaxIsWithheldFromEachPaymentOnTheWholeHolding()
    {
        // Seven ROR bonds pay one coupon of 7 * 0.33 = 2.31 zloty. The withholding
        // is ceil(0.19 * 2.31) = 0.44 zloty on the payment, and not seven times the
        // one-bond tax of 0.07 zloty. After three months two coupons are paid.
        var rows = Evaluate(TestIssues.Ror(), units: 7);
        var details = rows[2].Details!;

        Assert.Equal(Money.RoundToGrosz(0.88m), details.Settlement.CouponTax);
    }

    [Fact]
    public void ScalingTheHoldingScalesEveryGrossAmount()
    {
        var one = Evaluate(TestIssues.Coi(), units: 1);
        var hundred = Evaluate(TestIssues.Coi(), units: 100);

        for (var index = 0; index < one.Length; index++)
        {
            if (one[index].Details is null)
            {
                Assert.Null(hundred[index].Details);
                continue;
            }

            Assert.Equal(one[index].Details!.RedemptionGross * 100, hundred[index].Details!.RedemptionGross);
            Assert.Equal(one[index].Details!.CouponsGross * 100, hundred[index].Details!.CouponsGross);
        }
    }

    [Fact]
    public void RefusesAHoldingOfLessThanOneBond()
    {
        var plan = BondPlan.Create(TestIssues.Coi(), TestIssues.Purchase, market);

        Assert.Throws<ArgumentOutOfRangeException>(() => evaluator.Evaluate(plan, 0));
    }

    [Fact]
    public void MarksAMonthThatTheLetterDoesNotAllow()
    {
        // A window that blocks every call shows that the guard works, and that a
        // blocked month carries no figures.
        var issue = TestIssues.Ror() with { EarlyRedemption = new EarlyRedemptionWindow(400, 20) };
        var rows = Evaluate(issue);

        Assert.All(rows.Where(r => r.MonthIndex < 12), r => Assert.Equal(ExitKind.NotAvailable, r.Kind));
        Assert.Equal(ExitKind.Maturity, rows[11].Kind);
    }

    private ImmutableArray<ExitOutcome> Evaluate(BondIssue issue, int units = 1) =>
        evaluator.Evaluate(BondPlan.Create(issue, TestIssues.Purchase, market), units);
}
