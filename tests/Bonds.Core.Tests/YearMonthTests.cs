using Xunit;

namespace Bonds.Core.Tests;

public sealed class YearMonthTests
{
    [Fact]
    public void ParsesTheYearAndMonth()
    {
        var month = YearMonth.Parse("2026-07");
        Assert.Equal(new YearMonth(2026, 7), month);
    }

    [Theory]
    [InlineData("2026-13")]
    [InlineData("2026")]
    [InlineData("07-2026")]
    public void RejectsTextThatIsNotAMonth(string text) =>
        Assert.Throws<FormatException>(() => YearMonth.Parse(text));

    [Fact]
    public void StepsBackOverAYearBoundary() =>
        Assert.Equal(new YearMonth(2025, 12), new YearMonth(2026, 1).AddMonths(-1));

    [Fact]
    public void OrdersByYearThenMonth()
    {
        Assert.True(new YearMonth(2025, 12) < new YearMonth(2026, 1));
        Assert.True(new YearMonth(2026, 2) > new YearMonth(2026, 1));
    }

    [Fact]
    public void TextRoundTrips() => Assert.Equal("2026-07", YearMonth.Parse("2026-07").ToString());
}
