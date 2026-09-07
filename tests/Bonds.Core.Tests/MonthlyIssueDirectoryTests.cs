using Bonds.Core.Data;
using Bonds.Core.Tests.Support;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class MonthlyIssueDirectoryTests : IDisposable
{
    private readonly string temporaryDirectory =
        Path.Combine(Path.GetTempPath(), "bonds-months-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Theory]
    [InlineData(2026, 8, 1, "2026-08")]
    [InlineData(2026, 8, 31, "2026-08")]
    [InlineData(2026, 9, 1, "2026-09")]
    [InlineData(2026, 9, 30, "2026-09")]
    public void PicksTheSubdirectoryOfThePurchaseMonth(int year, int month, int day, string expected)
    {
        var resolved = MonthlyIssueDirectory.For(
            RepositoryPaths.Issues, new DateOnly(year, month, day));

        Assert.Equal(RepositoryPaths.IssuesFor(expected), resolved);
    }

    [Fact]
    public void NamesTheMonthsItHasWhenThePurchaseMonthIsMissing()
    {
        var error = Assert.Throws<DataLoadException>(
            () => MonthlyIssueDirectory.For(RepositoryPaths.Issues, new DateOnly(2026, 12, 1)));

        Assert.Contains("2026-12", error.Message, StringComparison.Ordinal);
        Assert.Contains("2026-08", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UsesADirectoryOfIssueFilesAsItIs()
    {
        // A path straight to one month must keep working, whatever the purchase
        // date. The sale window of each file is what refuses a wrong date, and its
        // error names the window.
        var august = RepositoryPaths.IssuesFor("2026-08");

        Assert.Equal(august, MonthlyIssueDirectory.For(august, new DateOnly(2026, 12, 1)));
    }

    [Fact]
    public void PrefersLooseIssueFilesOverMonthSubdirectories()
    {
        // A flat directory of one's own stays a flat directory, even when it holds
        // an unrelated subdirectory that looks like a month.
        Directory.CreateDirectory(Path.Combine(temporaryDirectory, "2026-09"));
        File.WriteAllText(Path.Combine(temporaryDirectory, "issue.json"), "{}");

        Assert.Equal(temporaryDirectory,
            MonthlyIssueDirectory.For(temporaryDirectory, new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public void LeavesADirectoryWithNeitherFilesNorMonthsToTheLoader()
    {
        // The loader raises "no *.json file" with the path in it. Two errors for
        // one empty directory would only be confusing.
        Directory.CreateDirectory(temporaryDirectory);

        Assert.Equal(temporaryDirectory,
            MonthlyIssueDirectory.For(temporaryDirectory, new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public void LeavesAMissingDirectoryToTheLoader()
    {
        var missing = Path.Combine(temporaryDirectory, "issues");

        Assert.Equal(missing, MonthlyIssueDirectory.For(missing, new DateOnly(2026, 9, 1)));
    }
}
