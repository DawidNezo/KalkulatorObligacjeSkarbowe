using System.Globalization;

namespace Bonds.Core.Data;

/// <summary>
/// Picks the directory of issue files that a purchase date belongs to. The
/// Ministry issues a new set of bonds every month and sells each set for one
/// calendar month only. A directory named after that month therefore holds
/// exactly the issues that a purchase in it can reach.
/// </summary>
public static class MonthlyIssueDirectory
{
    private const string MonthFormat = "yyyy-MM";

    /// <summary>
    /// Returns the directory to read the issues from.
    /// <para>
    /// A directory that holds *.json files is used as it is. This keeps a direct
    /// path such as "data/issues/2026-09" working, and it keeps a flat directory
    /// of one's own working.
    /// </para>
    /// <para>
    /// A directory that holds subdirectories named "yyyy-MM" instead is a
    /// collection of months. The subdirectory of the purchase month is returned.
    /// </para>
    /// </summary>
    public static string For(string directory, DateOnly purchase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory) || Directory.GetFiles(directory, "*.json").Length > 0)
        {
            return directory;
        }

        var months = MonthsIn(directory);
        if (months.Count == 0)
        {
            return directory;
        }

        var wanted = YearMonth.Of(purchase);
        if (months.TryGetValue(wanted, out var path))
        {
            return path;
        }

        throw new DataLoadException(directory,
            $"nie ma katalogu emisji na {wanted}, więc nie wiadomo, co jest w sprzedaży "
            + $"{purchase:yyyy-MM-dd}; dostępne miesiące: {string.Join(", ", months.Keys.Order())}");
    }

    /// <summary>The "yyyy-MM" subdirectories of a directory, by their month.</summary>
    private static SortedDictionary<YearMonth, string> MonthsIn(string directory)
    {
        var months = new SortedDictionary<YearMonth, string>();
        foreach (var candidate in Directory.GetDirectories(directory))
        {
            var name = Path.GetFileName(candidate);
            if (DateOnly.TryParseExact($"{name}-01", $"{MonthFormat}-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var first))
            {
                months[YearMonth.Of(first)] = candidate;
            }
        }

        return months;
    }
}
