using System.Globalization;
using System.Text;
using Bonds.Core.Engine;

namespace Bonds.Core.Reporting;

/// <summary>
/// A step by step account of one bond: every interest period, its rate, where the
/// rate comes from and what the bond is worth at the end of the period. Use it to
/// find where a number differs from the number of the issue agent.
/// </summary>
public sealed class ExplainWriter
{
    /// <summary>Report text must never depend on the locale of the machine.</summary>
    private static readonly IFormatProvider Fixed = CultureInfo.InvariantCulture;

    private readonly RedemptionCalculator redemption;

    public ExplainWriter(RedemptionCalculator redemption)
    {
        ArgumentNullException.ThrowIfNull(redemption);
        this.redemption = redemption;
    }

    public string Render(CalculationReport report, string code)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var bond = report.Bonds.FirstOrDefault(
                       b => string.Equals(b.Plan.Issue.Code, code, StringComparison.OrdinalIgnoreCase))
                   ?? throw new ArgumentException($"Raport nie zawiera emisji o kodzie '{code}'.");

        var issue = bond.Plan.Issue;
        var text = new StringBuilder();

        text.AppendLine(Fixed, $"# {ReportFormat.MarkdownCell(issue.Code)} — przebieg naliczania")
            .AppendLine()
            .AppendLine(Fixed, $"- Źródło: {issue.Source}")
            .AppendLine(Fixed, $"- Zakup: {ReportFormat.Date(bond.Plan.Purchase)}, "
                        + $"wykup: {ReportFormat.Date(bond.Plan.Maturity)}")
            .AppendLine(Fixed, $"- Nominał: {ReportFormat.Amount(issue.Nominal)} zł, "
                        + $"cena zakupu: {ReportFormat.Amount(issue.PurchasePrice)} zł")
            .AppendLine(Fixed, $"- Okresów odsetkowych: {issue.PeriodCount}, "
                        + $"po {issue.PeriodMonths} mies. (F = {issue.PeriodsPerYear})")
            .AppendLine(Fixed, $"- Oprocentowanie: {ReportFormat.DescribeRate(issue.Rate)}")
            .AppendLine(Fixed, $"- Kapitalizacja: {(issue.Capitalizes ? "roczna" : "brak")}"
                        + $", zaokrąglanie bazy: {(issue.RoundCapitalizedBase ? "tak" : "nie")}")
            .AppendLine(Fixed, $"- Podłoga 100 zł: {issue.Floor}")
            .AppendLine(Fixed, $"- Opłata: {ReportFormat.DescribeFee(issue.Fee)}")
            .AppendLine(Fixed, $"- Scenariusz: {report.Scenario.Name}")
            .AppendLine();

        text.AppendLine("| Okres | Początek | Koniec | ACT | Stopa | Źródło stopy | Kupon | Wartość na koniec |")
            .AppendLine("|---|---|---|---|---|---|---|---|");

        foreach (var period in bond.Plan.Periods)
        {
            var settlement = redemption.Settle(bond.Plan, period.Period.End);
            string[] cells =
            [
                (period.Period.Index + 1).ToString(Fixed),
                ReportFormat.Date(period.Period.Start),
                ReportFormat.Date(period.Period.End),
                period.Period.ActualDays.ToString(Fixed),
                ReportFormat.Rate(period.AnnualRate),
                period.IsProjected ? "prognoza" : "dane",
                ReportFormat.Amount(redemption.Coupon(bond.Plan, period.Period.Index)),
                ReportFormat.Amount(settlement.BondValue),
            ];
            text.AppendLine("| " + string.Join(" | ", cells) + " |");
        }

        var maturityValue = ReportFormat.Amount(redemption.MaturityValue(bond.Plan));
        text.AppendLine()
            .AppendLine(Fixed, $"Wartość w dniu wykupu: **{maturityValue} zł** na jedną obligację, przed podatkiem.")
            .AppendLine();

        return text.ToString();
    }
}
