using Bonds.Core;
using Bonds.Sources.Market;
using Bonds.Sources.Tests.Support;
using Xunit;

namespace Bonds.Sources.Tests;

/// <summary>
/// Reads the workbook that the repository ships. A crafted .xlsx would only prove
/// that the reader reads what this test writes; the real file is the one whose
/// shape can change under us.
/// </summary>
public sealed class CpiWorkbookTests
{
    [Fact]
    public void ReadsEveryMonthOfTheShippedWorkbook()
    {
        var readings = CpiWorkbook.Read(RepositoryPaths.CpiWorkbook);

        // One reading per month, from the first to the last, with no gap.
        var first = readings.Keys.Min();
        var last = readings.Keys.Max();
        var months = ((last.Year - first.Year) * 12) + last.Month - first.Month + 1;

        Assert.Equal(months, readings.Count);
    }

    [Fact]
    public void TakesTheBaseOffTheIndex()
    {
        // The workbook writes "analogiczny miesiąc poprzedniego roku = 100", thus
        // 103.0 means a growth of 3.00 per cent. A reader that forgets the base
        // would turn every rate into something above a hundred per cent.
        var readings = CpiWorkbook.Read(RepositoryPaths.CpiWorkbook);

        Assert.All(readings.Values, value => Assert.InRange(value, -20m, 40m));
    }

    [Fact]
    public void KeepsTheOneDecimalThatStatisticsPolandPublishes()
    {
        var readings = CpiWorkbook.Read(RepositoryPaths.CpiWorkbook);

        // A spreadsheet writes 100.70000000000001 for 100.7. Rounding to one
        // decimal before the subtraction removes that noise, thus every value has
        // at most one decimal.
        Assert.All(readings.Values,
            value => Assert.Equal(value, Math.Round(value, CpiWorkbook.IndexDecimals)));
    }

    [Fact]
    public void ReadsTheMonthsAsTheMonthsTheFiguresAreAbout()
    {
        var readings = CpiWorkbook.Read(RepositoryPaths.CpiWorkbook);

        // The keys are reference months, not publication months. The shift belongs
        // to MarketSeriesWriter, thus the last key must still be the last row of
        // the sheet and not one month later.
        Assert.True(readings.Keys.Max() <= YearMonth.Of(DateOnly.FromDateTime(DateTime.Today)));
    }

    [Fact]
    public void NamesTheSheetsItHasWhenTheOneItWantsIsMissing()
    {
        using var workbook = OpenXmlWorkbook.Open(RepositoryPaths.CpiWorkbook);

        var error = Assert.Throws<SourceException>(() => workbook.NumberRowsOf("nie_ma_takiego"));

        Assert.Contains("nie ma arkusza", error.Message, StringComparison.Ordinal);
        Assert.Contains(CpiWorkbook.SheetName, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsAMissingFile()
    {
        var error = Assert.Throws<SourceException>(
            () => CpiWorkbook.Read(Path.Combine(Path.GetTempPath(), "nie-ma-" + Guid.NewGuid() + ".xlsx")));

        Assert.Contains("nie istnieje", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsAFileThatIsNotAWorkbook()
    {
        // The XML archive of NBP is a file that a person could pass by mistake.
        var error = Assert.Throws<SourceException>(
            () => CpiWorkbook.Read(RepositoryPaths.NbpArchive));

        Assert.Contains("nie jest arkuszem", error.Message, StringComparison.Ordinal);
    }
}
