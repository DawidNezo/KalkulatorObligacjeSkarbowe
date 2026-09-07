using System.Collections.Immutable;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Bonds.Sources.Market;

/// <summary>
/// Reads the archive of NBP interest rates. The file holds one block per decision
/// of the Monetary Policy Council, and one entry per rate inside it. Only the
/// reference rate matters here, because the letters of ROR and DOR name it.
/// <para>
/// Download: https://nbp.pl/podstawowe-stopy-procentowe-archiwum/, the "plik XML"
/// link.
/// </para>
/// </summary>
public static class NbpReferenceArchive
{
    /// <summary>The identifier of the reference rate in the archive.</summary>
    public const string ReferenceRateId = "ref";

    private const string BlockElement = "pozycje";
    private const string EntryElement = "pozycja";
    private const string EffectiveAttribute = "obowiazuje_od";
    private const string RateAttribute = "oprocentowanie";

    /// <summary>
    /// The reference rate, in percent, by the day the decision takes effect.
    /// </summary>
    public static ImmutableSortedDictionary<DateOnly, decimal> Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new SourceException(path, "plik nie istnieje");
        }

        XDocument document;
        try
        {
            document = XDocument.Load(path);
        }
        catch (XmlException error)
        {
            throw new SourceException(path, $"nie można odczytać XML ({error.Message})", error);
        }

        var rates = ImmutableSortedDictionary.CreateBuilder<DateOnly, decimal>();
        foreach (var block in document.Descendants(BlockElement))
        {
            var effective = (string?)block.Attribute(EffectiveAttribute)
                ?? throw new SourceException(path, $"blok <{BlockElement}> bez atrybutu '{EffectiveAttribute}'");

            foreach (var entry in block.Elements(EntryElement))
            {
                if ((string?)entry.Attribute("id") != ReferenceRateId)
                {
                    continue;
                }

                var date = ParseDate(effective, path);
                if (rates.ContainsKey(date))
                {
                    throw new SourceException(path, $"data {date:yyyy-MM-dd} występuje więcej niż raz");
                }

                rates.Add(date, ParsePercent((string?)entry.Attribute(RateAttribute), path));
            }
        }

        if (rates.Count == 0)
        {
            throw new SourceException(path, $"nie ma ani jednej pozycji id=\"{ReferenceRateId}\"");
        }

        return rates.ToImmutable();
    }

    private static DateOnly ParseDate(string text, string path) =>
        DateOnly.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date)
            ? date
            : throw new SourceException(path, $"'{text}' nie jest datą w formacie yyyy-MM-dd");

    /// <summary>
    /// Reads a percent written with a comma, as the archive writes it. The culture
    /// is stated so that the reader gives the same number on every machine.
    /// </summary>
    private static decimal ParsePercent(string? text, string path)
    {
        if (text is null)
        {
            throw new SourceException(path, $"pozycja bez atrybutu '{RateAttribute}'");
        }

        return decimal.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Number,
            CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new SourceException(path, $"'{text}' nie jest liczbą");
    }
}
