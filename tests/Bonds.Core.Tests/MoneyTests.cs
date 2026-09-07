using Xunit;

namespace Bonds.Core.Tests;

public sealed class MoneyTests
{
    [Theory]
    [InlineData(0.005, 1)]
    [InlineData(0.004, 0)]
    [InlineData(-0.005, -1)]
    [InlineData(1.005, 101)]
    [InlineData(100.494999, 10049)]
    [InlineData(4.7499, 475)]
    public void RoundToGroszTakesHalvesAwayFromZero(decimal zloty, long expectedGrosze) =>
        Assert.Equal(expectedGrosze, Money.RoundToGrosz(zloty).Grosze);

    [Theory]
    [InlineData(0.0627, 7)]
    [InlineData(1.0165, 102)]
    [InlineData(6.27, 627)]
    [InlineData(0.0001, 1)]
    [InlineData(0.00, 0)]
    public void CeilToGroszRoundsUpAsArticle63Paragraph1aDoes(decimal zloty, long expectedGrosze) =>
        Assert.Equal(expectedGrosze, Money.CeilToGrosz(zloty).Grosze);

    [Fact]
    public void ZlotyStaysExactBecauseGroszeAreWholeNumbers() =>
        Assert.Equal(1.23m, Money.FromGrosze(123).Zloty);

    [Fact]
    public void ScalingByUnitsIsExact()
    {
        // A coupon of 0.33 zloty on a holding of 1000 bonds is exactly 330 zloty.
        Assert.Equal(33_000L, (Money.FromGrosze(33) * 1000).Grosze);
        Assert.Equal(Money.Zero, Money.FromGrosze(33) * 0);
    }

    [Fact]
    public void ApplyingARateGivesADecimalSoThatTheCallerRounds()
    {
        var interest = Money.FromGrosze(10_000) * 0.19m;
        Assert.Equal(19.00m, interest);
    }

    [Fact]
    public void AdditionAndSubtractionStayInGrosze()
    {
        var sum = Money.FromGrosze(45) + Money.FromGrosze(55);
        Assert.Equal(Money.FromGrosze(100), sum);
        Assert.Equal(Money.FromGrosze(-10), Money.FromGrosze(45) - Money.FromGrosze(55));
    }

    [Fact]
    public void MinAndMaxComparePlainly()
    {
        var low = Money.FromGrosze(5);
        var high = Money.FromGrosze(50);
        Assert.Equal(low, Money.Min(low, high));
        Assert.Equal(high, Money.Max(low, high));
    }

    [Fact]
    public void TextIsCultureIndependent() => Assert.Equal("100.50", Money.FromGrosze(10_050).ToString());
}
