using System.Globalization;
using Bonds.Core.CommandLine;
using Bonds.Core.Reporting;
using static Bonds.Core.CommandLine.CommandLineArguments;

namespace Bonds.Cli;

/// <summary>The command that the user asked for.</summary>
public enum CliCommand
{
    /// <summary>One exit table per issue.</summary>
    Table,

    /// <summary>One matrix with a row per month and a column per issue.</summary>
    Matrix,

    /// <summary>A step by step account of one issue.</summary>
    Explain,
}

/// <summary>
/// Parses the command line. The parser is written by hand, thus the tool needs no
/// package and cannot break when a package changes.
/// </summary>
public sealed record CommandLineOptions(
    CliCommand Command,
    string IssuesDirectory,
    string ScenarioPath,
    string NbpSeriesPath,
    string CpiSeriesPath,
    DateOnly Purchase,
    int Units,
    decimal TaxRatePercent,
    string OutputFormat,
    string? OutputPath,
    IReadOnlyList<string> IssueCodes,
    ExitMetric Metric,
    bool ShowHelp)
{
    private const decimal DefaultTaxPercent = 19m;
    private const int DefaultUnits = 1;

    public decimal TaxRate => TaxRatePercent / 100m;

    public static string Usage =>
        """
        bonds — kalkulator polskich obligacji skarbowych detalicznych

        Użycie:
          bonds                            to samo co 'bonds table'
          bonds table   [opcje]            tabela wyjścia w każdym miesiącu, dla każdej emisji
          bonds matrix  [opcje]            macierz miesiąc x emisja dla jednej metryki
          bonds explain --issue KOD        przebieg naliczania jednej emisji

        Opcje:
          --purchase RRRR-MM-DD   data zakupu (domyślnie dzisiaj)
          --units N               liczba obligacji w pozycji (domyślnie 1)
          --tax PROCENT           stawka podatku w procentach (domyślnie 19)
          --issue KOD             ogranicz do emisji; można podać wielokrotnie lub po przecinku
          --format md|csv|json    format wyjścia; tylko dla komendy table (domyślnie md)
          --metric profit|rate|irr    metryka; tylko dla komendy matrix (domyślnie irr)
          --out PLIK              zapisz do pliku; bez tej opcji wynik idzie na standardowe wyjście
          --issues KATALOG        katalog z plikami emisji (domyślnie data/issues).
                                  Jeśli zawiera podkatalogi RRRR-MM, brany jest ten
                                  zgodny z miesiącem zakupu
          --scenario PLIK         plik scenariusza (domyślnie data/scenarios/base.json)
          --nbp PLIK              seria stopy referencyjnej NBP (domyślnie data/market/nbp-reference.json)
          --cpi PLIK              seria inflacji CPI r/r (domyślnie data/market/cpi-yoy.json)
          -h, --help              ta pomoc

        Przykłady:
          bonds
          bonds --units 1000
          bonds matrix --metric irr
          bonds table --purchase 2026-08-17 --issue EDO0836 --format csv --out out/edo.csv
        """;

    /// <summary>
    /// Reads the command line. <paramref name="today"/> is the date to use when the
    /// caller gives no purchase date; it is a parameter so that a test stays
    /// independent of the clock. <paramref name="dataRoot"/> is the directory that
    /// the default data paths grow from, so that the tool works from any working
    /// directory.
    /// </summary>
    public static CommandLineOptions Parse(IReadOnlyList<string> arguments, DateOnly today, string dataRoot = "data")
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);

        // No command, or an option in the first place, means the table command.
        var command = CliCommand.Table;
        var firstOption = 0;
        if (arguments.Count > 0 && !arguments[0].StartsWith('-'))
        {
            command = ParseCommand(arguments[0]);
            firstOption = 1;
        }

        var issues = Path.Combine(dataRoot, "issues");
        var scenario = Path.Combine(dataRoot, "scenarios", "base.json");
        var nbp = Path.Combine(dataRoot, "market", "nbp-reference.json");
        var cpi = Path.Combine(dataRoot, "market", "cpi-yoy.json");
        DateOnly? purchase = null;
        var units = DefaultUnits;
        var taxPercent = DefaultTaxPercent;
        var format = "md";
        var formatGiven = false;
        var metricGiven = false;
        string? output = null;
        var codes = new List<string>();
        var metric = ExitMetric.AnnualisedNetReturn;
        var help = false;

        for (var index = firstOption; index < arguments.Count; index++)
        {
            var name = arguments[index];
            switch (name)
            {
                case "-h" or "--help": help = true; break;
                case "--issues": issues = Value(arguments, ref index); break;
                case "--scenario": scenario = Value(arguments, ref index); break;
                case "--nbp": nbp = Value(arguments, ref index); break;
                case "--cpi": cpi = Value(arguments, ref index); break;
                case "--purchase": purchase = ParseDate(Value(arguments, ref index), name); break;
                case "--units": units = ParseCount(Value(arguments, ref index)); break;
                case "--tax": taxPercent = ParseDecimal(Value(arguments, ref index), name); break;
                case "--format":
                    format = Value(arguments, ref index).ToLowerInvariant();
                    formatGiven = true;
                    break;
                case "--out": output = Value(arguments, ref index); break;
                case "--metric":
                    metric = ParseMetric(Value(arguments, ref index));
                    metricGiven = true;
                    break;
                case "--issue":
                    codes.AddRange(Value(arguments, ref index)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;
                default:
                    throw new CommandLineException($"Nieznana opcja '{name}'.");
            }
        }

        var options = new CommandLineOptions(
            command, issues, scenario, nbp, cpi, purchase ?? today, units, taxPercent,
            format, output, codes, metric, help);

        // Help skips the checks below: "bonds explain --help" must show the help
        // and not complain about a missing --issue.
        return help ? options : options.EnsureConsistent(formatGiven, metricGiven);
    }

    private CommandLineOptions EnsureConsistent(bool formatGiven, bool metricGiven)
    {
        if (Command == CliCommand.Explain && IssueCodes.Count != 1)
        {
            throw new CommandLineException("Komenda 'explain' wymaga dokładnie jednej opcji --issue KOD.");
        }

        if (OutputFormat is not ("md" or "csv" or "json"))
        {
            throw new CommandLineException($"Nieznany format '{OutputFormat}'. Dostępne: md, csv, json.");
        }

        if (formatGiven && Command != CliCommand.Table)
        {
            throw new CommandLineException("Opcja --format działa tylko z komendą table.");
        }

        if (metricGiven && Command != CliCommand.Matrix)
        {
            throw new CommandLineException("Opcja --metric działa tylko z komendą matrix.");
        }

        return this;
    }

    private static CliCommand ParseCommand(string text) => text switch
    {
        "table" => CliCommand.Table,
        "matrix" => CliCommand.Matrix,
        "explain" => CliCommand.Explain,
        _ => throw new CommandLineException($"Nieznana komenda '{text}'."),
    };

    private static ExitMetric ParseMetric(string text) => text.ToLowerInvariant() switch
    {
        "profit" => ExitMetric.NetProfit,
        "rate" => ExitMetric.NetProfitRate,
        "irr" => ExitMetric.AnnualisedNetReturn,
        _ => throw new CommandLineException($"Nieznana metryka '{text}'. Dostępne: profit, rate, irr."),
    };

    private static int ParseCount(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 1
            ? value
            : throw new CommandLineException($"'{text}' nie jest liczbą całkowitą większą od zera.");

    private static decimal ParseDecimal(string text, string option) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new CommandLineException($"Opcja '{option}' wymaga liczby; '{text}' nią nie jest.");
}
