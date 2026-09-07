using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Bonds.Core;

namespace Bonds.Sources.Market;

/// <summary>
/// Renders the two market series that the calculator reads. The rendered text is
/// the whole product of this tool, thus it is a value and not a file: a test can
/// check it without a disk.
/// </summary>
public static class MarketSeriesWriter
{
    /// <summary>
    /// The lag between the month a CPI figure is about and the month it comes out.
    /// Statistics Poland publishes the figure for month M in month M+1. The letters
    /// name "the index published in the month before the first month of the
    /// interest period", thus the series has to be indexed by the month of
    /// publication and not by the month it is about.
    /// </summary>
    public const int CpiPublicationLagMonths = 1;

    private const string DayFormat = "yyyy-MM-dd";

    /// <summary>Two decimals, as the letters and the communiques write a percent.</summary>
    private const string PercentFormat = "0.00";

    public static string RenderNbpReference(
        ImmutableSortedDictionary<DateOnly, decimal> rates, DateOnly knownThrough, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(rates);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        var text = new StringBuilder();
        text.AppendLine("{");
        text.AppendLine(CultureInfo.InvariantCulture,
            $"  // Wygenerowane przez ./bonds-dane z pliku {sourceName}.");
        text.AppendLine("  // Zmiany rób w pliku źródłowym i uruchom skrypt jeszcze raz.");
        text.AppendLine("  //");
        text.AppendLine("  // Klucz = dzień wejścia w życie decyzji RPP, wartość = stopa referencyjna");
        text.AppendLine("  // w procentach. 'knownThrough' to ostatni dzień, dla którego stopa jest");
        text.AppendLine("  // znana: archiwum wymienia wszystkie decyzje podjęte do dziś, więc każdy");
        text.AppendLine("  // dzień do tej daty ma stopę ogłoszoną, a każdy późniejszy zależy od");
        text.AppendLine("  // decyzji, której jeszcze nie ma. Zapytanie o dzień późniejszy zwraca");
        text.AppendLine("  // wartość ze scenariusza i jest w raporcie oznaczone gwiazdką.");
        text.AppendLine("  \"name\": \"Stopa referencyjna NBP\",");
        text.AppendLine(CultureInfo.InvariantCulture,
            $"  \"knownThrough\": \"{knownThrough.ToString(DayFormat, CultureInfo.InvariantCulture)}\",");
        text.AppendLine("  \"percent\": {");
        AppendEntries(text, rates.Select(rate =>
            (rate.Key.ToString(DayFormat, CultureInfo.InvariantCulture), rate.Value)));
        text.AppendLine("  }");
        text.AppendLine("}");
        return text.ToString();
    }

    /// <summary>
    /// Renders the CPI series. The keys move forward by
    /// <see cref="CpiPublicationLagMonths"/>, from the month a figure is about to
    /// the month it comes out.
    /// </summary>
    public static string RenderCpi(
        ImmutableSortedDictionary<YearMonth, decimal> readings, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(readings);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        if (readings.IsEmpty)
        {
            throw new ArgumentException("Seria CPI nie zawiera żadnego odczytu.", nameof(readings));
        }

        var published = readings.ToImmutableSortedDictionary(
            reading => reading.Key.AddMonths(CpiPublicationLagMonths),
            reading => reading.Value);

        var text = new StringBuilder();
        text.AppendLine("{");
        text.AppendLine(CultureInfo.InvariantCulture,
            $"  // Wygenerowane przez ./bonds-dane z pliku {sourceName}");
        text.AppendLine(CultureInfo.InvariantCulture,
            $"  // (arkusz '{CpiWorkbook.SheetName}', kolumna CPI; obliczenia NBP na danych GUS).");
        text.AppendLine("  // Zmiany rób w pliku źródłowym i uruchom skrypt jeszcze raz.");
        text.AppendLine("  //");
        text.AppendLine("  // Klucz = miesiąc OGŁOSZENIA wskaźnika przez GUS, nie miesiąc, którego");
        text.AppendLine("  // wskaźnik dotyczy: GUS ogłasza wskaźnik za miesiąc M w miesiącu M+1,");
        text.AppendLine("  // a listy emisyjne każą brać „wskaźnik ogłaszany w miesiącu poprzedzającym");
        text.AppendLine(CultureInfo.InvariantCulture,
            $"  // pierwszy miesiąc danego okresu odsetkowego”. Klucz {published.Keys.Max()} to więc");
        text.AppendLine(CultureInfo.InvariantCulture,
            $"  // odczyt za {readings.Keys.Max()}.");
        text.AppendLine("  //");
        text.AppendLine("  // Wartość = inflacja r/r w procentach. Arkusz podaje ją jako");
        text.AppendLine("  // „analogiczny miesiąc poprzedniego roku = 100”, więc skrypt odjął 100.");
        text.AppendLine("  \"name\": \"Inflacja CPI r/r wg miesiąca ogłoszenia GUS\",");
        text.AppendLine(CultureInfo.InvariantCulture,
            $"  \"knownThrough\": \"{published.Keys.Max()}\",");
        text.AppendLine("  \"percent\": {");
        AppendEntries(text, published.Select(month => (month.Key.ToString(), month.Value)));
        text.AppendLine("  }");
        text.AppendLine("}");
        return text.ToString();
    }

    /// <summary>
    /// Writes the entries of a JSON object, with a comma after every one but the
    /// last. The whole series is one object, thus the separator cannot be handled
    /// by the caller.
    /// </summary>
    private static void AppendEntries(StringBuilder text, IEnumerable<(string Key, decimal Value)> entries)
    {
        var lines = entries
            .Select(entry => string.Create(CultureInfo.InvariantCulture,
                $"    \"{entry.Key}\": {entry.Value.ToString(PercentFormat, CultureInfo.InvariantCulture)}"))
            .ToList();

        for (var index = 0; index < lines.Count; index++)
        {
            text.Append(lines[index]);
            text.AppendLine(index == lines.Count - 1 ? string.Empty : ",");
        }
    }
}
