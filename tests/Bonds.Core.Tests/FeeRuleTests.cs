using Bonds.Core.Domain;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class FeeRuleTests
{
    private static readonly Money Fee = Money.RoundToGrosz(0.50m);

    [Fact]
    public void TwoTierCapsTheFeeInTheFirstPeriod()
    {
        var rule = new TwoTierFee(Fee);

        Assert.Equal(Money.RoundToGrosz(0.05m), rule.Compute(0, Money.RoundToGrosz(0.05m)));
        Assert.Equal(Fee, rule.Compute(0, Money.RoundToGrosz(0.90m)));
    }

    [Fact]
    public void TwoTierChargesTheWholeFeeFromTheSecondPeriod()
    {
        var rule = new TwoTierFee(Fee);

        // The payment can now fall below the face value, because the floor of the
        // ROR, DOR and COI letters covers the first period only.
        Assert.Equal(Fee, rule.Compute(1, Money.RoundToGrosz(0.05m)));
    }

    [Fact]
    public void AccruedCappedNeverExceedsTheInterestEarned()
    {
        var rule = new AccruedCappedFee(Money.RoundToGrosz(3.00m));

        Assert.Equal(Money.RoundToGrosz(0.45m), rule.Compute(0, Money.RoundToGrosz(0.45m)));
        Assert.Equal(Money.RoundToGrosz(3.00m), rule.Compute(4, Money.RoundToGrosz(25.00m)));
        Assert.Equal(Money.RoundToGrosz(0.10m), rule.Compute(4, Money.RoundToGrosz(0.10m)));
    }

    [Fact]
    public void ForfeitAllInterestTakesExactlyTheInterest()
    {
        var rule = new ForfeitAllInterestFee();

        Assert.Equal(Money.RoundToGrosz(0.17m), rule.Compute(0, Money.RoundToGrosz(0.17m)));
        Assert.Equal(Money.Zero, rule.Compute(0, Money.Zero));
    }
}
