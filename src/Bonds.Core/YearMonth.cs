using System.Globalization;

namespace Bonds.Core;

/// <summary>A calendar month. The CPI series is indexed by the month of publication.</summary>
public readonly record struct YearMonth(int Year, int Month) : IComparable<YearMonth>
{
    private const string TextFormat = "yyyy-MM";

    public static YearMonth Of(DateOnly date) => new(date.Year, date.Month);

    public static YearMonth Parse(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (!DateOnly.TryParseExact(text + "-01", TextFormat + "-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            throw new FormatException($"'{text}' nie jest miesiącem w formacie {TextFormat}.");
        }

        return Of(date);
    }

    public YearMonth AddMonths(int months) => Of(FirstDay.AddMonths(months));

    public DateOnly FirstDay => new(Year, Month, 1);

    private int Ordinal => (Year * 12) + Month - 1;

    public int CompareTo(YearMonth other) => Ordinal.CompareTo(other.Ordinal);

    public static bool operator <(YearMonth a, YearMonth b) => a.Ordinal < b.Ordinal;

    public static bool operator >(YearMonth a, YearMonth b) => a.Ordinal > b.Ordinal;

    public static bool operator <=(YearMonth a, YearMonth b) => a.Ordinal <= b.Ordinal;

    public static bool operator >=(YearMonth a, YearMonth b) => a.Ordinal >= b.Ordinal;

    public override string ToString() => FirstDay.ToString(TextFormat, CultureInfo.InvariantCulture);
}
