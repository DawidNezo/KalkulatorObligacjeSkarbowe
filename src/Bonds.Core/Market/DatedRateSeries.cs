using System.Collections.Immutable;

namespace Bonds.Core.Market;

/// <summary>
/// A step function of rates over time, such as the NBP reference rate. A point
/// holds from its own date until the next point. Data after
/// <see cref="KnownThrough"/> is not published yet.
/// </summary>
public sealed class DatedRateSeries
{
    private readonly ImmutableArray<(DateOnly From, decimal Value)> points;

    public DatedRateSeries(string name, DateOnly knownThrough,
        IEnumerable<KeyValuePair<DateOnly, decimal>> points)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(points);

        Name = name;
        KnownThrough = knownThrough;
        this.points = [.. points.Select(p => (p.Key, p.Value)).OrderBy(p => p.Key)];

        if (this.points.IsEmpty)
        {
            throw new ArgumentException($"Seria '{name}' nie zawiera żadnego punktu.", nameof(points));
        }
    }

    public string Name { get; }

    /// <summary>The last date for which the series holds published data.</summary>
    public DateOnly KnownThrough { get; }

    public DateOnly EarliestDate => points[0].From;

    /// <summary>
    /// The rate in force on the date given. Returns null when the date is beyond
    /// the published data, so that the caller can put an assumption in its place.
    /// </summary>
    public decimal? ValueOn(DateOnly date)
    {
        if (date > KnownThrough)
        {
            return null;
        }

        if (date < EarliestDate)
        {
            throw new ArgumentOutOfRangeException(nameof(date),
                $"Seria '{Name}' zaczyna się {EarliestDate:yyyy-MM-dd}, więc nie zna wartości dla {date:yyyy-MM-dd}. "
                + "Uzupełnij serię o wcześniejsze punkty albo przesuń datę zakupu.");
        }

        var value = points[0].Value;
        foreach (var point in points)
        {
            if (point.From > date)
            {
                break;
            }

            value = point.Value;
        }

        return value;
    }
}
