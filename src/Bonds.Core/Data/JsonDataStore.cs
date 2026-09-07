using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bonds.Core.Domain;
using Bonds.Core.Market;

namespace Bonds.Core.Data;

/// <summary>
/// Reads the issue, market and scenario files. Every value that a file misses
/// raises a <see cref="DataLoadException"/> that names the file and the field, so
/// that a wrong file never turns into a wrong number. A field that no document
/// declares, a duplicate key and a gap in a monthly series all fail the same way.
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static",
    Justification = "These methods hold no state today. They stay instance methods because the class is injected as a collaborator, and a static method cannot be substituted in a test nor replaced by another implementation.")]
public sealed class JsonDataStore
{
    private const decimal PercentDivisor = 100m;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(), new UniqueKeyMapConverter() },
    };

    /// <summary>Reads every *.json file of a directory as one bond issue, sorted by code.</summary>
    public IReadOnlyList<BondIssue> LoadIssues(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DataLoadException(directory, "katalog emisji nie istnieje");
        }

        var files = Directory.GetFiles(directory, "*.json").OrderBy(f => f, StringComparer.Ordinal).ToList();
        if (files.Count == 0)
        {
            throw new DataLoadException(directory, "katalog emisji nie zawiera żadnego pliku *.json");
        }

        var issues = files.Select(LoadIssue).OrderBy(i => i.Code, StringComparer.Ordinal).ToList();

        var duplicates = issues.GroupBy(i => i.Code, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToList();
        if (duplicates.Count > 0)
        {
            throw new DataLoadException(directory,
                $"kod emisji powtarza się w kilku plikach: {string.Join(", ", duplicates)}");
        }

        return issues;
    }

    public BondIssue LoadIssue(string path)
    {
        var document = Read<IssueDocument>(path);
        var code = Required(document.Code, path, nameof(document.Code));
        var sale = Required(document.Sale, path, nameof(document.Sale));

        var issue = new BondIssue(
            code,
            ParseEnum<BondKind>(document.Kind, path, nameof(document.Kind)),
            Money.RoundToGrosz(Required(document.NominalZloty, path, nameof(document.NominalZloty))),
            Money.RoundToGrosz(Required(document.PurchasePriceZloty, path, nameof(document.PurchasePriceZloty))),
            Required(document.PeriodsPerYear, path, nameof(document.PeriodsPerYear)),
            Required(document.PeriodCount, path, nameof(document.PeriodCount)),
            Required(document.Capitalizes, path, nameof(document.Capitalizes)),
            Required(document.RoundCapitalizedBase, path, nameof(document.RoundCapitalizedBase)),
            ParseEnum<PrincipalFloor>(document.PrincipalFloor, path, nameof(document.PrincipalFloor)),
            ToRateRule(Required(document.Rate, path, nameof(document.Rate)), path),
            ToFeeRule(Required(document.Fee, path, nameof(document.Fee)), path),
            ToWindow(Required(document.EarlyRedemption, path, nameof(document.EarlyRedemption)), path),
            Required(sale.From, path, "Sale.From"),
            Required(sale.To, path, "Sale.To"),
            Required(document.Source, path, nameof(document.Source)));

        try
        {
            return issue.Validated();
        }
        catch (ArgumentException error)
        {
            throw new DataLoadException(path, ErrorMessages.WithoutParameterName(error.Message), error);
        }
    }

    public Scenario LoadScenario(string path)
    {
        var document = Read<ScenarioDocument>(path);
        return new Scenario(
            Required(document.Name, path, nameof(document.Name)),
            Required(document.AssumedCpiPercent, path, nameof(document.AssumedCpiPercent)) / PercentDivisor,
            Required(document.AssumedNbpReferencePercent, path, nameof(document.AssumedNbpReferencePercent))
                / PercentDivisor);
    }

    public DatedRateSeries LoadDatedSeries(string path)
    {
        var document = Read<DatedSeriesDocument>(path);
        var percent = Required(document.Percent, path, nameof(document.Percent));
        var knownThrough = Required(document.KnownThrough, path, nameof(document.KnownThrough));

        var points = new Dictionary<DateOnly, decimal>();
        foreach (var (key, value) in percent)
        {
            if (!DateOnly.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
            {
                throw new DataLoadException(path, $"'{key}' nie jest datą w formacie yyyy-MM-dd");
            }

            if (date > knownThrough)
            {
                throw new DataLoadException(path,
                    $"punkt {date:yyyy-MM-dd} leży za 'knownThrough' ({knownThrough:yyyy-MM-dd})");
            }

            points[date] = value / PercentDivisor;
        }

        try
        {
            return new DatedRateSeries(
                Required(document.Name, path, nameof(document.Name)), knownThrough, points);
        }
        catch (ArgumentException error)
        {
            throw new DataLoadException(path, ErrorMessages.WithoutParameterName(error.Message), error);
        }
    }

    public MonthlyRateSeries LoadMonthlySeries(string path)
    {
        var document = Read<MonthlySeriesDocument>(path);
        var percent = Required(document.Percent, path, nameof(document.Percent));
        var knownThrough = ParseMonth(
            Required(document.KnownThrough, path, nameof(document.KnownThrough)), path);

        var points = new Dictionary<YearMonth, decimal>();
        foreach (var (key, value) in percent)
        {
            points[ParseMonth(key, path)] = value / PercentDivisor;
        }

        EnsureMonthlySeriesHasNoGap(points, knownThrough, path);

        try
        {
            return new MonthlyRateSeries(
                Required(document.Name, path, nameof(document.Name)), knownThrough, points);
        }
        catch (ArgumentException error)
        {
            throw new DataLoadException(path, ErrorMessages.WithoutParameterName(error.Message), error);
        }
    }

    /// <summary>
    /// A monthly series must cover every month from its first point through
    /// "knownThrough". A gap would surface much later, in the middle of a
    /// calculation, and a point beyond "knownThrough" would never be read.
    /// </summary>
    private static void EnsureMonthlySeriesHasNoGap(
        Dictionary<YearMonth, decimal> points, YearMonth knownThrough, string path)
    {
        if (points.Count == 0)
        {
            return;
        }

        foreach (var month in points.Keys.Where(month => month > knownThrough))
        {
            throw new DataLoadException(path, $"miesiąc {month} leży za 'knownThrough' ({knownThrough})");
        }

        var first = points.Keys.Min();
        for (var month = first; month <= knownThrough; month = month.AddMonths(1))
        {
            if (!points.ContainsKey(month))
            {
                throw new DataLoadException(path,
                    $"w serii brakuje miesiąca {month}, choć 'knownThrough' to {knownThrough}");
            }
        }
    }

    private static YearMonth ParseMonth(string text, string path)
    {
        try
        {
            return YearMonth.Parse(text);
        }
        catch (FormatException error)
        {
            throw new DataLoadException(path, error.Message, error);
        }
    }

    private static RateRule ToRateRule(RateDocument document, string path)
    {
        var type = Required(document.Type, path, "Rate.Type");
        var first = Required(document.FirstPeriodRatePercent, path, "Rate.FirstPeriodRatePercent")
            / PercentDivisor;

        return type switch
        {
            "fixed" => new FixedRate(first),
            "nbpReference" => new NbpReferenceLinkedRate(first, Margin(document, path)),
            "inflation" => new InflationLinkedRate(first, Margin(document, path)),
            _ => throw new DataLoadException(path,
                $"'{type}' nie jest znanym typem oprocentowania; dostępne: fixed, nbpReference, inflation"),
        };
    }

    private static decimal Margin(RateDocument document, string path) =>
        Required(document.MarginPercent, path, "Rate.MarginPercent") / PercentDivisor;

    private static FeeRule ToFeeRule(FeeDocument document, string path)
    {
        var type = Required(document.Type, path, "Fee.Type");
        return type switch
        {
            "twoTier" => new TwoTierFee(FeeAmount(document, path)),
            "accruedCapped" => new AccruedCappedFee(FeeAmount(document, path)),
            "forfeitAllInterest" => new ForfeitAllInterestFee(),
            _ => throw new DataLoadException(path,
                $"'{type}' nie jest znanym typem opłaty; dostępne: twoTier, accruedCapped, forfeitAllInterest"),
        };
    }

    private static Money FeeAmount(FeeDocument document, string path) =>
        Money.RoundToGrosz(Required(document.AmountZloty, path, "Fee.AmountZloty"));

    private static EarlyRedemptionWindow ToWindow(EarlyRedemptionDocument document, string path) =>
        new(Required(document.MinDaysAfterPurchase, path, "EarlyRedemption.MinDaysAfterPurchase"),
            Required(document.MinDaysBeforeMaturity, path, "EarlyRedemption.MinDaysBeforeMaturity"));

    private static T Read<T>(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new DataLoadException(path, "plik nie istnieje");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options)
                   ?? throw new DataLoadException(path, "plik nie zawiera obiektu");
        }
        catch (JsonException error)
        {
            throw new DataLoadException(path, $"nie można odczytać pliku ({error.Message})", error);
        }
    }

    private static T Required<T>(T? value, string path, string field)
        where T : class =>
        value ?? throw new DataLoadException(path, $"brak pola '{field}'");

    private static T Required<T>(T? value, string path, string field)
        where T : struct =>
        value ?? throw new DataLoadException(path, $"brak pola '{field}'");

    private static TEnum ParseEnum<TEnum>(string? value, string path, string field)
        where TEnum : struct, Enum
    {
        var text = Required(value, path, field);
        return Enum.TryParse<TEnum>(text, ignoreCase: true, out var parsed)
            ? parsed
            : throw new DataLoadException(path,
                $"'{text}' nie jest poprawną wartością {typeof(TEnum).Name}; dostępne: {string.Join(", ", Enum.GetNames<TEnum>())}");
    }

    /// <summary>
    /// Reads a JSON object as a map and refuses a key that appears twice. The
    /// standard reader keeps the last value silently, and a duplicated date in a
    /// hand-edited market file must fail instead.
    /// </summary>
    private sealed class UniqueKeyMapConverter : JsonConverter<Dictionary<string, decimal>>
    {
        public override Dictionary<string, decimal> Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("oczekiwano obiektu JSON");
            }

            var map = new Dictionary<string, decimal>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var key = reader.GetString() ?? throw new JsonException("oczekiwano nazwy pola");
                if (!reader.Read())
                {
                    throw new JsonException("oczekiwano wartości pola");
                }

                if (!map.TryAdd(key, reader.GetDecimal()))
                {
                    throw new JsonException($"klucz '{key}' występuje więcej niż raz");
                }
            }

            return map;
        }

        public override void Write(
            Utf8JsonWriter writer, Dictionary<string, decimal> value, JsonSerializerOptions options) =>
            throw new NotSupportedException("The store only reads these files.");
    }
}
