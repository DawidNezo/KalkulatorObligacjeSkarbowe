using System.Globalization;
using System.Text;

namespace Bonds.Core.Reporting;

/// <summary>
/// The comparison matrix as CSV: a row for each month, a column for each issue,
/// in the same dialect as <see cref="CsvReportWriter"/>. A spreadsheet can freeze
/// the header row and hide columns, which a Markdown table cannot. A month in
/// which an issue no longer exists, or the metric does not, is an empty cell.
/// </summary>
public sealed class CsvMatrixWriter
{
    /// <summary>Report text must never depend on the locale of the machine.</summary>
    private static readonly IFormatProvider Fixed = CultureInfo.InvariantCulture;

    private const char Separator = ReportFormat.CsvSeparator;

    /// <summary>The columns before the issue codes.</summary>
    private static readonly string[] LeadingColumns = ["miesiac", "data_wyjscia"];

    private readonly ExitMetric metric;

    public CsvMatrixWriter(ExitMetric metric) => this.metric = metric;

    public string Render(CalculationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.Bonds.IsEmpty)
        {
            throw new ArgumentException("Raport nie zawiera żadnej obligacji.", nameof(report));
        }

        var lastMonth = report.Bonds.Max(b => b.Plan.Issue.TermMonths);

        var text = new StringBuilder();
        text.AppendLine(string.Join(Separator,
            LeadingColumns
                .Concat(report.Bonds.Select(b => b.Plan.Issue.Code))
                .Select(ReportFormat.CsvCell)));

        for (var month = 1; month <= lastMonth; month++)
        {
            var date = report.Purchase.AddMonths(month);
            var cells = new[] { month.ToString(Fixed), ReportFormat.Date(date) }
                .Concat(report.Bonds.Select(bond => Cell(bond, month)));
            text.AppendLine(string.Join(Separator, cells.Select(ReportFormat.CsvCell)));
        }

        return text.ToString();
    }

    private string Cell(BondReport bond, int month)
    {
        var exit = bond.Exits.FirstOrDefault(e => e.MonthIndex == month);
        if (exit?.Details is null)
        {
            return string.Empty;
        }

        var value = ReportFormat.MetricValue(metric, exit.Details.Settlement);
        return value == ReportFormat.Dash ? string.Empty : value;
    }
}
