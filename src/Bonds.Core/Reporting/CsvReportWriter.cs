using System.Globalization;
using System.Text;
using Bonds.Core.Engine;

namespace Bonds.Core.Reporting;

/// <summary>
/// One flat CSV table for every issue. The separator is a comma and the decimal
/// mark stays a Polish comma, thus every amount and rate is a quoted field.
/// </summary>
public sealed class CsvReportWriter : IReportWriter
{
    /// <summary>Report text must never depend on the locale of the machine.</summary>
    private static readonly IFormatProvider Fixed = CultureInfo.InvariantCulture;

    private const char Separator = ReportFormat.CsvSeparator;

    private static readonly string[] Columns =
    [
        "emisja", "miesiac", "data_wyjscia", "sposob", "stopa_okresu", "stopa_prognozowana",
        "kupony_brutto", "odsetki_w_wykupie", "oplata", "wyplata_brutto",
        "podatek", "zysk_netto", "zysk_procent", "irr_netto",
    ];

    public string Format => "csv";

    public string Render(CalculationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var text = new StringBuilder();
        text.AppendLine(string.Join(Separator, Columns));
        foreach (var bond in report.Bonds)
        {
            foreach (var exit in bond.Exits)
            {
                text.AppendLine(string.Join(Separator, Cells(bond.Plan.Issue.Code, exit).Select(ReportFormat.CsvCell)));
            }
        }

        return text.ToString();
    }

    private static IEnumerable<string> Cells(string code, ExitOutcome exit)
    {
        yield return code;
        yield return exit.MonthIndex.ToString(Fixed);
        yield return ReportFormat.Date(exit.ExitDate);
        yield return ReportFormat.Kind(exit.Kind);

        if (exit.Details is null)
        {
            for (var column = 4; column < Columns.Length; column++)
            {
                yield return string.Empty;
            }

            yield break;
        }

        var details = exit.Details;
        yield return ReportFormat.Rate(details.PeriodRate);
        yield return details.RateIsProjected ? "tak" : "nie";
        yield return ReportFormat.Amount(details.CouponsGross);
        yield return ReportFormat.Amount(details.RedemptionInterestGross);
        yield return ReportFormat.Amount(details.Fee);
        yield return ReportFormat.Amount(details.TotalReceiptsGross);
        yield return ReportFormat.Amount(details.Settlement.TotalTax);
        yield return ReportFormat.Amount(details.Settlement.NetProfit);
        yield return ReportFormat.Rate(details.Settlement.NetProfitRate);
        yield return ReportFormat.RateOrDash(details.Settlement.AnnualisedNetReturn);
    }
}
