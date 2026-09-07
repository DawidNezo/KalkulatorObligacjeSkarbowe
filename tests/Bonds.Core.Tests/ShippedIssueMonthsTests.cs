using Bonds.Core.Data;
using Bonds.Core.Domain;
using Bonds.Core.Tests.Support;
using Xunit;

namespace Bonds.Core.Tests;

/// <summary>
/// Guards the layout of data/issues. Every sale month is one subdirectory named
/// "yyyy-MM", and the whole monthly routine rests on the name of the directory
/// telling the truth about the sale window inside its files. A file dropped into
/// the wrong month would otherwise show up much later, as a purchase date that
/// the calculator refuses.
/// </summary>
public sealed class ShippedIssueMonthsTests
{
    private readonly JsonDataStore store = new();

    /// <summary>The eight kinds that the Ministry offers every month.</summary>
    private static readonly BondKind[] EveryKind =
        [BondKind.Ots, BondKind.Ror, BondKind.Dor, BondKind.Tos,
         BondKind.Coi, BondKind.Ros, BondKind.Edo, BondKind.Rod];

    public static TheoryData<string> Months()
    {
        var months = new TheoryData<string>();
        foreach (var month in RepositoryPaths.IssueMonths)
        {
            months.Add(month);
        }

        return months;
    }

    [Fact]
    public void TheRepositoryShipsAtLeastOneSaleMonth() =>
        Assert.NotEmpty(RepositoryPaths.IssueMonths);

    [Fact]
    public void EverySubdirectoryOfIssuesIsNamedAfterItsSaleMonth() =>
        Assert.All(RepositoryPaths.IssueMonths,
            month => Assert.Equal(month, YearMonth.Parse(month).ToString()));

    [Theory]
    [MemberData(nameof(Months))]
    public void EveryShippedMonthHoldsTheEightKinds(string month)
    {
        var issues = store.LoadIssues(RepositoryPaths.IssuesFor(month));

        Assert.Equal(EveryKind.Order(), issues.Select(i => i.Kind).Order());
    }

    [Theory]
    [MemberData(nameof(Months))]
    public void EveryIssueIsOnSaleForTheWholeMonthOfItsDirectory(string month)
    {
        var expected = YearMonth.Parse(month);

        Assert.All(store.LoadIssues(RepositoryPaths.IssuesFor(month)), issue =>
        {
            Assert.Equal(expected.FirstDay, issue.SaleStart);
            Assert.Equal(expected.AddMonths(1).FirstDay.AddDays(-1), issue.SaleEnd);
        });
    }

    [Theory]
    [MemberData(nameof(Months))]
    public void EveryIssueNamesTheLetterItComesFrom(string month) =>
        Assert.All(store.LoadIssues(RepositoryPaths.IssuesFor(month)),
            issue => Assert.Contains("List emisyjny", issue.Source, StringComparison.Ordinal));

    [Fact]
    public void NoCodeIsSoldInTwoMonths()
    {
        var codes = RepositoryPaths.IssueMonths
            .SelectMany(month => store.LoadIssues(RepositoryPaths.IssuesFor(month)))
            .Select(issue => issue.Code)
            .ToList();

        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
    }
}
