using System.Collections.Immutable;
using System.Globalization;
using Bonds.Core;

namespace Bonds.Sources.Market;

/// <summary>
/// Reads the yearly CPI growth from the core inflation workbook of NBP. The CPI
/// column of that workbook is the figure that Statistics Poland publishes, and NBP
/// states the source under the table.
/// <para>
/// Download: https://nbp.pl/statystyka-i-sprawozdawczosc/inflacja-bazowa/
/// </para>
/// <para>
/// The keys are the months that the figures are ABOUT. The move to the month of
/// publication happens in <see cref="MarketSeriesWriter"/>, because that move is a
/// rule of the issue letters and not a property of the workbook.
/// </para>
/// </summary>
public static class CpiWorkbook
{
    /// <summary>The sheet with one row per month.</summary>
    public const string SheetName = "dane_miesieczne";

    /// <summary>The column with the date of the row.</summary>
    public const string DateColumn = "A";

    /// <summary>The column with the CPI index, previous year = 100.</summary>
    public const string IndexColumn = "B";

    /// <summary>
    /// The base of the index. The workbook writes 103.0 for a growth of 3.00 per
    /// cent, thus the base comes off.
    /// </summary>
    public const decimal IndexBase = 100m;

    /// <summary>
    /// Statistics Poland states the index to one decimal. Rounding to it removes
    /// the binary noise of a value that a spreadsheet wrote as 100.70000000000001.
    /// </summary>
    public const int IndexDecimals = 1;

    /// <summary>
    /// The day that serial number zero means. A spreadsheet counts days from
    /// 1899-12-30 for compatibility with Lotus 1-2-3.
    /// </summary>
    private static readonly DateOnly SerialEpoch = new(1899, 12, 30);

    /// <summary>The yearly CPI growth in percent, by the month it is about.</summary>
    public static ImmutableSortedDictionary<YearMonth, decimal> Read(string path)
    {
        using var workbook = OpenXmlWorkbook.Open(path);

        var readings = ImmutableSortedDictionary.CreateBuilder<YearMonth, decimal>();
        foreach (var cells in workbook.NumberRowsOf(SheetName))
        {
            if (!cells.TryGetValue(DateColumn, out var serial)
                || !cells.TryGetValue(IndexColumn, out var index)
                || !TryReadRow(serial, index, out var month, out var percent))
            {
                // The header rows and the row that names the source also hold
                // values in these columns. A row that does not read as a date and
                // a number is not a reading.
                continue;
            }

            if (readings.ContainsKey(month))
            {
                throw new SourceException(path, $"miesiąc {month} występuje więcej niż raz");
            }

            readings.Add(month, percent);
        }

        if (readings.Count == 0)
        {
            throw new SourceException(path, $"arkusz '{SheetName}' nie zawiera ani jednego odczytu");
        }

        EnsureNoGap(readings, path);
        return readings.ToImmutable();
    }

    private static bool TryReadRow(string serial, string index, out YearMonth month, out decimal percent)
    {
        month = default;
        percent = default;

        if (!decimal.TryParse(serial, NumberStyles.Number, CultureInfo.InvariantCulture, out var days)
            || !decimal.TryParse(index, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            || days <= 0
            || days > int.MaxValue)
        {
            return false;
        }

        month = YearMonth.Of(SerialEpoch.AddDays((int)days));
        percent = Math.Round(value, IndexDecimals) - IndexBase;
        return true;
    }

    /// <summary>
    /// A monthly series must have every month between its first and its last. The
    /// calculator refuses a series with a gap, and a gap that only shows up there
    /// would be far away from its cause.
    /// </summary>
    private static void EnsureNoGap(
        ImmutableSortedDictionary<YearMonth, decimal>.Builder readings, string path)
    {
        var last = readings.Keys.Max();
        for (var month = readings.Keys.Min(); month <= last; month = month.AddMonths(1))
        {
            if (!readings.ContainsKey(month))
            {
                throw new SourceException(path,
                    $"brakuje odczytu za {month}, choć dane sięgają {last}");
            }
        }
    }
}
