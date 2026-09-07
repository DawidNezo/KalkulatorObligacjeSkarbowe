using System.Globalization;
using System.Text;
using Bonds.Core.Engine;

namespace Bonds.Core.Reporting;

/// <summary>One Markdown section per issue, with one row per month of exit.</summary>
public sealed class MarkdownReportWriter : IReportWriter
{
    /// <summary>Report text must never depend on the locale of the machine.</summary>
    private static readonly IFormatProvider Fixed = CultureInfo.InvariantCulture;

    private static readonly string[] Columns =
    [
        "M", "Data wyjścia", "Sposób", "Stopa okresu", "Kupony brutto", "Odsetki w wykupie",
        "Opłata", "Wypłata brutto", "Podatek", "Zysk netto", "Zysk %", "IRR netto",
    ];

    public string Format => "md";

    public string Render(CalculationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var text = new StringBuilder();
        WriteHeader(text, report);
        foreach (var bond in report.Bonds)
        {
            WriteBond(text, bond);
        }

        WriteLegend(text);
        return text.ToString();
    }

    private static void WriteHeader(StringBuilder text, CalculationReport report)
    {
        var taxRate = ReportFormat.Rate(report.TaxRate);
        text.AppendLine("# Polskie obligacje skarbowe detaliczne — wyjście w każdym miesiącu")
            .AppendLine()
            .AppendLine(Fixed, $"- Data zakupu: {ReportFormat.Date(report.Purchase)}")
            .AppendLine(Fixed, $"- Pozycja: {ReportFormat.Units(report.Units)}")
            .AppendLine(Fixed, $"- Podatek: {taxRate} od odsetek, zaokrąglany w górę do grosza (art. 63 §1a Ordynacji podatkowej); opłata za przedterminowy wykup pomniejsza podstawę")
            .AppendLine(Fixed, $"- Scenariusz: {report.Scenario.Name}")
            .AppendLine(Fixed, $"- Założona inflacja CPI: {ReportFormat.Rate(report.Scenario.AssumedCpi)}; "
                        + $"założona stopa referencyjna NBP: {ReportFormat.Rate(report.Scenario.AssumedNbpReference)}")
            .AppendLine();
    }

    private static void WriteBond(StringBuilder text, BondReport bond)
    {
        var issue = bond.Plan.Issue;
        var projected = bond.Plan.FirstProjectedPeriod;

        text.AppendLine(Fixed, $"## {ReportFormat.MarkdownCell(issue.Code)} — {issue.Kind}, {issue.TermMonths} miesięcy")
            .AppendLine()
            .AppendLine(Fixed, $"- Źródło: {issue.Source}")
            .AppendLine(Fixed, $"- Wykup: {ReportFormat.Date(bond.Plan.Maturity)}")
            .AppendLine(Fixed, $"- Oprocentowanie: {ReportFormat.DescribeRate(issue.Rate)}")
            .AppendLine(Fixed, $"- Kapitalizacja: {(issue.Capitalizes ? "roczna" : "brak, odsetki wypłacane")}")
            .AppendLine(Fixed, $"- Opłata za przedterminowy wykup: {ReportFormat.DescribeFee(issue.Fee)}")
            .AppendLine(projected is null
                ? "- Wszystkie stopy z danych opublikowanych"
                : $"- Prognoza od okresu {projected.Period.Index + 1} "
                  + $"({ReportFormat.Date(projected.Period.Start)})")
            .AppendLine();

        text.AppendLine("| " + string.Join(" | ", Columns) + " |");
        text.AppendLine("|" + string.Join("|", Columns.Select(_ => "---")) + "|");
        foreach (var exit in bond.Exits)
        {
            text.AppendLine("| " + string.Join(" | ", Cells(exit)) + " |");
        }

        text.AppendLine();
    }

    private static IEnumerable<string> Cells(ExitOutcome exit)
    {
        yield return exit.MonthIndex.ToString(Fixed);
        yield return ReportFormat.Date(exit.ExitDate);
        yield return ReportFormat.Kind(exit.Kind);

        if (exit.Details is null)
        {
            for (var column = 3; column < Columns.Length; column++)
            {
                yield return ReportFormat.Dash;
            }

            yield break;
        }

        var details = exit.Details;
        yield return ReportFormat.Rate(details.PeriodRate) + (details.RateIsProjected ? "*" : string.Empty);
        yield return ReportFormat.Amount(details.CouponsGross);
        yield return ReportFormat.Amount(details.RedemptionInterestGross);
        yield return ReportFormat.Amount(details.Fee);
        yield return ReportFormat.Amount(details.TotalReceiptsGross);
        yield return ReportFormat.Amount(details.Settlement.TotalTax);
        yield return ReportFormat.Amount(details.Settlement.NetProfit);
        yield return ReportFormat.Rate(details.Settlement.NetProfitRate);
        yield return ReportFormat.RateOrDash(details.Settlement.AnnualisedNetReturn);
    }

    private static void WriteLegend(StringBuilder text)
    {
        text.AppendLine("## Legenda")
            .AppendLine()
            .AppendLine("- **Kupony brutto** — odsetki wypłacone przed datą wyjścia (ROR, DOR i COI).")
            .AppendLine("- **Odsetki w wykupie** — odsetki zawarte w wypłacie z wykupu, przed opłatą; "
                        + "dla obligacji kapitalizujących to całość odsetek.")
            .AppendLine("- **Wypłata brutto** — kupony plus wypłata z wykupu po opłacie, przed podatkiem.")
            .AppendLine("- **Podatek** — pobierany z każdej wypłaty dla całej pozycji i zaokrąglany "
                        + "w górę do grosza (art. 63 §1a Ordynacji podatkowej).")
            .AppendLine("- **IRR netto** — roczna wewnętrzna stopa zwrotu z przepływów netto; "
                        + "jedyna porównywalna miara między obligacjami kuponowymi i kapitalizującymi.")
            .AppendLine("- `*` przy stopie — wartość z prognozy, nie z danych opublikowanych.")
            .AppendLine("- **niedostępny** w kolumnie Sposób — dnia wyjścia nie obejmuje okno "
                        + "przedterminowego wykupu z listu emisyjnego.")
            .AppendLine();
    }
}
