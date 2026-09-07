using Bonds.Core.Domain;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class EarlyRedemptionWindowTests
{
    private static readonly EarlyRedemptionWindow Window = new(7, 20);
    private static readonly DateOnly Purchase = new(2026, 8, 17);
    private static readonly DateOnly Maturity = new(2027, 8, 17);

    [Fact]
    public void AllowsACallAfterSevenCalendarDays()
    {
        Assert.False(Window.Allows(Purchase, Maturity, Purchase.AddDays(7)));
        Assert.True(Window.Allows(Purchase, Maturity, Purchase.AddDays(8)));
    }

    [Fact]
    public void BlocksACallInsideTheLastTwentyCalendarDays()
    {
        Assert.True(Window.Allows(Purchase, Maturity, Maturity.AddDays(-20)));
        Assert.False(Window.Allows(Purchase, Maturity, Maturity.AddDays(-19)));
    }

    [Fact]
    public void AllowsAnOrdinaryMonthInTheMiddle() =>
        Assert.True(Window.Allows(Purchase, Maturity, Purchase.AddMonths(6)));
}
