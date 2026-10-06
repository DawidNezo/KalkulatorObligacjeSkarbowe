using System.Globalization;
using Bonds.Core.Domain;
using Bonds.Core.Engine;

namespace Bonds.Core.Reporting;

/// <summary>
/// Shared text formatting. Amounts and rates use Polish decimal commas and no
/// group separator, thus the output stays stable and easy to paste into a sheet.
/// </summary>
internal static class ReportFormat
{
    internal const string Dash = "—";

    private const string AmountPattern = "0.00";
    private const string RatePattern = "0.00";
    private const decimal PercentFactor = 100m;

    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    internal static string Amount(Money value) => value.Zloty.ToString(AmountPattern, Polish);

    internal static string Rate(decimal value) =>
        (value * PercentFactor).ToString(RatePattern, Polish) + "%";

    internal static string RateOrDash(decimal? value) => value is null ? Dash : Rate(value.Value);

    internal static string Date(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Polish declension: 1 obligacja, 2–4 obligacje, 5+ obligacji (12–14 obligacji).</summary>
    internal static string Units(int count)
    {
        var word = count == 1
            ? "obligacja"
            : count % 10 is >= 2 and <= 4 && count % 100 is < 12 or > 14
                ? "obligacje"
                : "obligacji";
        return string.Create(CultureInfo.InvariantCulture, $"{count} {word}");
    }

    /// <summary>A pipe inside a Markdown table cell would end the cell.</summary>
    internal static string MarkdownCell(string text) => text.Replace("|", "\\|", StringComparison.Ordinal);

    /// <summary>
    /// True for text that this file writes as a number: an amount or a rate, with
    /// or without the percent sign. A CSV cell that fails this test and starts
    /// with a minus is not a negative profit but text that a spreadsheet could
    /// run as a formula.
    /// </summary>
    internal static bool IsNumber(string text) =>
        decimal.TryParse(text.TrimEnd('%'), NumberStyles.Number, Polish, out _);

    /// <summary>
    /// The CSV field separator. A comma, as in RFC 4180, because the GitHub file
    /// viewer reads nothing else; a value with a decimal comma gets quoted.
    /// </summary>
    internal const char CsvSeparator = ',';

    /// <summary>
    /// Quotes a field that would break the table and disarms a field that a
    /// spreadsheet would run as a formula. The data files are hand-edited, thus a
    /// stray comma or a leading "=" in a code must not corrupt the output.
    /// A leading minus is dangerous only on text: a negative profit is a plain
    /// number and stays untouched.
    /// </summary>
    internal static string CsvCell(string cell)
    {
        var formula = cell.Length > 0
            && (cell[0] is '=' or '+' or '@' or '\t'
                || (cell[0] is '-' && !IsNumber(cell)));
        var safe = formula ? "'" + cell : cell;
        return safe.Contains(CsvSeparator, StringComparison.Ordinal)
               || safe.Contains('"', StringComparison.Ordinal)
               || safe.Contains('\n', StringComparison.Ordinal)
               || safe.Contains('\r', StringComparison.Ordinal)
            ? "\"" + safe.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : safe;
    }

    internal static string Kind(ExitKind kind) => kind switch
    {
        ExitKind.EarlyRedemption => "wykup przedterminowy",
        ExitKind.Maturity => "wykup",
        ExitKind.NotAvailable => "niedostępny",
        ExitKind.DoesNotExist => "nie istnieje",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown exit kind."),
    };

    internal static string DescribeRate(RateRule rule) => rule switch
    {
        FixedRate fixedRate => $"stała {Rate(fixedRate.AnnualRate)}",
        NbpReferenceLinkedRate nbp =>
            $"okres 1: {Rate(nbp.FirstRate)}, dalej stopa referencyjna NBP + {Rate(nbp.Margin)}",
        InflationLinkedRate cpi =>
            $"okres 1: {Rate(cpi.FirstRate)}, dalej CPI r/r + {Rate(cpi.Margin)}",
        _ => rule.GetType().Name,
    };

    internal static string DescribeFee(FeeRule rule) => rule switch
    {
        TwoTierFee twoTier => $"{Amount(twoTier.Fee)} zł (w 1. okresie nie więcej niż narosłe odsetki)",
        AccruedCappedFee capped => $"{Amount(capped.Fee)} zł, nie więcej niż narosłe odsetki",
        ForfeitAllInterestFee => "bez opłaty, przepadają wszystkie odsetki",
        _ => rule.GetType().Name,
    };

    internal static string DescribeMetric(ExitMetric metric) => metric switch
    {
        ExitMetric.NetProfit => "zysk netto (zł)",
        ExitMetric.NetProfitRate => "zysk netto (%)",
        ExitMetric.AnnualisedNetReturn => "IRR netto (rocznie)",
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, "Unknown metric."),
    };

    internal static string MetricValue(ExitMetric metric, ExitSettlement settlement) => metric switch
    {
        ExitMetric.NetProfit => Amount(settlement.NetProfit),
        ExitMetric.NetProfitRate => Rate(settlement.NetProfitRate),
        ExitMetric.AnnualisedNetReturn => RateOrDash(settlement.AnnualisedNetReturn),
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, "Unknown metric."),
    };
}
