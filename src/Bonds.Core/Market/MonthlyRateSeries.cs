using System.Collections.Immutable;

namespace Bonds.Core.Market;

/// <summary>
/// A rate per calendar month, such as the yearly CPI growth indexed by the month
/// of its publication. Unlike a step function, a month with no entry has no value.
/// </summary>
public sealed class MonthlyRateSeries
{
    private readonly ImmutableDictionary<YearMonth, decimal> values;

    public MonthlyRateSeries(string name, YearMonth knownThrough,
        IEnumerable<KeyValuePair<YearMonth, decimal>> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(values);

        Name = name;
        KnownThrough = knownThrough;
        this.values = values.ToImmutableDictionary();

        if (this.values.IsEmpty)
        {
            throw new ArgumentException($"Seria '{name}' nie zawiera żadnego punktu.", nameof(values));
        }
    }

    public string Name { get; }

    /// <summary>The last month for which the series holds published data.</summary>
    public YearMonth KnownThrough { get; }

    /// <summary>
    /// The rate published in the month given. Returns null when the month is beyond
    /// the published data, so that the caller can put an assumption in its place.
    /// </summary>
    public decimal? ValueIn(YearMonth month)
    {
        if (month > KnownThrough)
        {
            return null;
        }

        if (values.TryGetValue(month, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException(
            $"Seria '{Name}' nie ma wartości dla miesiąca {month}, choć jej dane sięgają {KnownThrough}. "
            + "Uzupełnij serię o wcześniejsze miesiące albo przesuń datę zakupu.");
    }
}
