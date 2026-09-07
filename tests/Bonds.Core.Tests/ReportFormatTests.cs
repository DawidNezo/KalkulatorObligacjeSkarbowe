using Bonds.Core.Reporting;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class ReportFormatTests
{
    [Theory]
    [InlineData(1, "1 obligacja")]
    [InlineData(2, "2 obligacje")]
    [InlineData(4, "4 obligacje")]
    [InlineData(5, "5 obligacji")]
    [InlineData(12, "12 obligacji")]
    [InlineData(14, "14 obligacji")]
    [InlineData(22, "22 obligacje")]
    [InlineData(100, "100 obligacji")]
    [InlineData(1000, "1000 obligacji")]
    public void UnitsFollowPolishDeclension(int count, string expected) =>
        Assert.Equal(expected, ReportFormat.Units(count));
}
