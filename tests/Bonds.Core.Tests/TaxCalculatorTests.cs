using Bonds.Core.Engine;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class TaxCalculatorTests
{
    private readonly TaxCalculator calculator = new(TaxCalculator.StandardRate);

    [Fact]
    public void RoundsTheTaxUpToAGrosz()
    {
        // 19 per cent of 0.33 zloty is 0.0627 zloty. Art. 63 par. 1a of the Tax
        // Ordinance Act rounds the tax on interest up, thus 0.07 and never 0.06.
        Assert.Equal(Money.RoundToGrosz(0.07m), calculator.Withhold(Money.RoundToGrosz(0.33m)));

        // 19 per cent of 5.35 zloty is 1.0165 zloty, thus 1.02.
        Assert.Equal(Money.RoundToGrosz(1.02m), calculator.Withhold(Money.RoundToGrosz(5.35m)));
    }

    [Fact]
    public void AnExactProductNeedsNoRounding() =>
        Assert.Equal(Money.RoundToGrosz(6.27m), calculator.Withhold(Money.RoundToGrosz(33.00m)));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-500)]
    public void ALossGivesNoTaxAndNoRefund(long grosze) =>
        Assert.Equal(Money.Zero, calculator.Withhold(Money.FromGrosze(grosze)));

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void RejectsARateOutsideZeroToOne(decimal rate) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new TaxCalculator(rate));
}
