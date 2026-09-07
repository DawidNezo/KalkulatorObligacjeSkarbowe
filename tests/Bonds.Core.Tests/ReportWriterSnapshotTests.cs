using System.Collections.Immutable;
using Bonds.Core.Data;
using Bonds.Core.Engine;
using Bonds.Core.Reporting;
using Bonds.Core.Tests.Support;
using Xunit;

namespace Bonds.Core.Tests;

/// <summary>
/// Locks the shape of every output. The per-issue snapshot covers four issues, one
/// for each combination of payment pattern and fee rule, which keeps the file small
/// enough to read. The matrix snapshot covers all eight issues.
/// </summary>
public sealed class ReportWriterSnapshotTests
{
    private static readonly Scenario Scenario = new("Snapshot: CPI 3,50%, NBP 4,00%", 0.0350m, 0.0400m);

    [Fact]
    public void MarkdownReportKeepsItsShape() =>
        SnapshotAssert.Matches("report.md", new MarkdownReportWriter().Render(FourIssues()));

    [Fact]
    public void CsvReportKeepsItsShape() =>
        SnapshotAssert.Matches("report.csv", new CsvReportWriter().Render(FourIssues()));

    [Fact]
    public void JsonReportKeepsItsShape() =>
        SnapshotAssert.Matches("report.json", new JsonReportWriter().Render(OneIssue()));

    [Fact]
    public void JsonReportOfACapitalisingBondKeepsItsShape() =>
        SnapshotAssert.Matches("report-edo.json", new JsonReportWriter().Render(Build([TestIssues.Edo()])));

    [Fact]
    public void ComparisonMatrixKeepsItsShape() =>
        SnapshotAssert.Matches("matrix-irr.md",
            new ComparisonMatrixWriter(ExitMetric.AnnualisedNetReturn).Render(AllIssues()));

    [Fact]
    public void ComparisonMatrixOfNetProfitKeepsItsShape() =>
        SnapshotAssert.Matches("matrix-profit.md",
            new ComparisonMatrixWriter(ExitMetric.NetProfit).Render(FourIssues()));

    [Fact]
    public void ComparisonMatrixShowsTheNetProfitRateAsAPercentage()
    {
        var text = new ComparisonMatrixWriter(ExitMetric.NetProfitRate).Render(OneIssue());

        Assert.Contains("zysk netto (%)", text, StringComparison.Ordinal);
        Assert.Contains("%", text.Split('\n').Last(line => line.StartsWith("| 3 ", StringComparison.Ordinal)),
            StringComparison.Ordinal);
    }

    [Fact]
    public void ExplainKeepsItsShape() =>
        SnapshotAssert.Matches("explain-edo.md",
            new ExplainWriter(new RedemptionCalculator()).Render(AllIssues(), "EDO0836"));

    [Fact]
    public void CsvDisarmsAFieldThatASpreadsheetWouldRunAsAFormula()
    {
        // The issue files are hand-edited; a code with a separator or a leading
        // "=" must not corrupt the table nor run as a formula in a spreadsheet.
        var report = Build([TestIssues.Ots() with { Code = "=SUMA();X" }]);

        var firstRow = new CsvReportWriter().Render(report).Split('\n')[1];

        Assert.StartsWith("\"'=SUMA();X\"", firstRow, StringComparison.Ordinal);
    }

    [Fact]
    public void TheMatrixEscapesAPipeInsideACode()
    {
        var report = Build([TestIssues.Ots() with { Code = "A|B" }]);

        var text = new ComparisonMatrixWriter(ExitMetric.NetProfit).Render(report);

        Assert.Contains("A\\|B", text, StringComparison.Ordinal);
        Assert.DoesNotContain("| A|B |", text, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryWriterNamesItsFormat()
    {
        var writers = new IReportWriter[]
        {
            new MarkdownReportWriter(), new CsvReportWriter(), new JsonReportWriter(),
        };

        Assert.Equal(["md", "csv", "json"], writers.Select(w => w.Format));
    }

    [Fact]
    public void ExplainRefusesAnIssueThatTheReportDoesNotHold() =>
        Assert.Throws<ArgumentException>(
            () => new ExplainWriter(new RedemptionCalculator()).Render(OneIssue(), "ZZZ0000"));

    [Fact]
    public void ExplainFindsAnIssueRegardlessOfTheCase()
    {
        var text = new ExplainWriter(new RedemptionCalculator()).Render(AllIssues(), "edo0836");

        Assert.Contains("EDO0836", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TheMatrixRefusesAnEmptyReport()
    {
        var empty = new CalculationReport(Scenario, TestIssues.Purchase, 1, 0.19m, []);

        Assert.Throws<ArgumentException>(
            () => new ComparisonMatrixWriter(ExitMetric.NetProfit).Render(empty));
    }

    private static CalculationReport OneIssue() => Build([TestIssues.Ots()]);

    private static CalculationReport FourIssues() =>
        Build([TestIssues.Ots(), TestIssues.Ror(), TestIssues.Tos(), TestIssues.Coi()]);

    private static CalculationReport AllIssues() => Build([.. TestIssues.All()]);

    private static CalculationReport Build(IReadOnlyList<Bonds.Core.Domain.BondIssue> issues)
    {
        var market = new FixedMarketData(Scenario.AssumedNbpReference, Scenario.AssumedCpi, projected: true);
        var evaluator = new ExitEvaluator(new RedemptionCalculator(), new TaxCalculator(0.19m));

        var reports = ImmutableArray.CreateBuilder<BondReport>(issues.Count);
        foreach (var issue in issues)
        {
            var plan = BondPlan.Create(issue, TestIssues.Purchase, market);
            reports.Add(new BondReport(plan, evaluator.Evaluate(plan, units: 100)));
        }

        return new CalculationReport(Scenario, TestIssues.Purchase, 100, 0.19m, reports.MoveToImmutable());
    }
}
