using System.Globalization;
using System.Text;

namespace Bonds.Core.Reporting;

/// <summary>
/// One Markdown table with a row for each month and a column for each issue. This
/// answers the question "which bond wins if I leave after N months".
/// </summary>
public sealed class ComparisonMatrixWriter
{
    /// <summary>Report text must never depend on the locale of the machine.</summary>
    private static readonly IFormatProvider Fixed = CultureInfo.InvariantCulture;

    private readonly ExitMetric metric;

    public ComparisonMatrixWriter(ExitMetric metric) => this.metric = metric;

    public string Render(CalculationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.Bonds.IsEmpty)
        {
            throw new ArgumentException("Raport nie zawiera żadnej obligacji.", nameof(report));
        }

        var codes = report.Bonds.Select(b => ReportFormat.MarkdownCell(b.Plan.Issue.Code)).ToList();
        var lastMonth = report.Bonds.Max(b => b.Plan.Issue.TermMonths);

        var text = new StringBuilder();
        text.AppendLine(Fixed, $"# Porównanie: {ReportFormat.DescribeMetric(metric)}")
            .AppendLine()
            .AppendLine(Fixed, $"- Data zakupu: {ReportFormat.Date(report.Purchase)}")
            .AppendLine(Fixed, $"- Pozycja: {ReportFormat.Units(report.Units)}")
            .AppendLine(Fixed, $"- Scenariusz: {report.Scenario.Name}")
            .AppendLine("- Kreska: obligacji już nie ma w tym miesiącu albo miara nie istnieje")
            .AppendLine();

        text.AppendLine("| M | Data wyjścia | " + string.Join(" | ", codes) + " |");
        text.AppendLine("|---|---|" + string.Join("|", codes.Select(_ => "---")) + "|");

        for (var month = 1; month <= lastMonth; month++)
        {
            var date = report.Purchase.AddMonths(month);
            var cells = report.Bonds.Select(bond => Cell(bond, month));
            text.AppendLine("| " + month.ToString(Fixed) + " | " + ReportFormat.Date(date) + " | "
                            + string.Join(" | ", cells) + " |");
        }

        return text.ToString();
    }

    private string Cell(BondReport bond, int month)
    {
        var exit = bond.Exits.FirstOrDefault(e => e.MonthIndex == month);
        return exit?.Details is null
            ? ReportFormat.Dash
            : ReportFormat.MetricValue(metric, exit.Details.Settlement);
    }
}
