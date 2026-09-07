using Bonds.Core.Engine;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class InternalRateOfReturnTests
{
    private static readonly DateOnly Start = new(2026, 8, 17);

    [Fact]
    public void FindsFivePerCentOnAGainOfFiveAfterOneYear()
    {
        var flows = new List<CashFlow>
        {
            new(Start, Money.RoundToGrosz(-100m)),
            new(Start.AddDays(365), Money.RoundToGrosz(105m)),
        };

        var rate = InternalRateOfReturn.Annualised(flows);

        Assert.NotNull(rate);
        Assert.Equal(0.05m, decimal.Round(rate.Value, 4));
    }

    [Fact]
    public void GivesZeroWhenNothingIsEarned()
    {
        var flows = new List<CashFlow>
        {
            new(Start, Money.RoundToGrosz(-100m)),
            new(Start.AddDays(60), Money.RoundToGrosz(100m)),
        };

        Assert.Equal(0m, InternalRateOfReturn.Annualised(flows));
    }

    [Fact]
    public void GivesANegativeRateOnALoss()
    {
        var flows = new List<CashFlow>
        {
            new(Start, Money.RoundToGrosz(-100m)),
            new(Start.AddDays(365), Money.RoundToGrosz(99m)),
        };

        var rate = InternalRateOfReturn.Annualised(flows);

        Assert.NotNull(rate);
        Assert.Equal(-0.01m, decimal.Round(rate.Value, 4));
    }

    [Fact]
    public void CountsMonthlyCouponsAsMoneyBackEarly()
    {
        // Twelve coupons of one zloty plus the face value beat one payment of 112
        // after a year, because the coupons come back sooner.
        var coupons = new List<CashFlow> { new(Start, Money.RoundToGrosz(-100m)) };
        for (var month = 1; month <= 12; month++)
        {
            coupons.Add(new CashFlow(Start.AddMonths(month), Money.RoundToGrosz(1m)));
        }

        coupons.Add(new CashFlow(Start.AddMonths(12), Money.RoundToGrosz(100m)));

        var lumpSum = new List<CashFlow>
        {
            new(Start, Money.RoundToGrosz(-100m)),
            new(Start.AddMonths(12), Money.RoundToGrosz(112m)),
        };

        Assert.True(InternalRateOfReturn.Annualised(coupons) > InternalRateOfReturn.Annualised(lumpSum));
    }

    [Fact]
    public void GivesNothingWhenEveryFlowHasTheSameSign()
    {
        var flows = new List<CashFlow>
        {
            new(Start, Money.RoundToGrosz(100m)),
            new(Start.AddDays(365), Money.RoundToGrosz(105m)),
        };

        Assert.Null(InternalRateOfReturn.Annualised(flows));
    }

    [Fact]
    public void GivesNothingForASingleFlow() =>
        Assert.Null(InternalRateOfReturn.Annualised(new List<CashFlow> { new(Start, Money.Zero) }));
}
