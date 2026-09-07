using System.Globalization;
using Bonds.Core.CommandLine;
using static Bonds.Core.CommandLine.CommandLineArguments;

namespace Bonds.Sources;

/// <summary>The command that the user asked for.</summary>
public enum SourcesCommand
{
    /// <summary>Rewrite the market series from the files of NBP and Statistics Poland.</summary>
    Market,

    /// <summary>Print the numbered paragraphs of one issue letter.</summary>
    Letter,
}

/// <summary>
/// Parses the command line. The parser is written by hand, for the same reason as
/// the one of the calculator: a package would add nothing and could break.
/// </summary>
public sealed record CommandLineOptions(
    SourcesCommand Command,
    string NbpArchivePath,
    string CpiWorkbookPath,
    string MarketDirectory,
    DateOnly? NbpKnownThrough,
    DateOnly Today,
    bool DryRun,
    string? LetterPath,
    bool FieldsOnly,
    bool AnnexesOnly,
    IReadOnlyList<int> WantedParagraphs,
    bool ShowHelp)
{
    public const string NbpArchiveName = "stopy_procentowe_archiwum.xml";
    public const string CpiWorkbookName = "bazowa.xlsx";

    public static string Usage =>
        """
        bonds-dane — przepisuje dokumenty źródłowe NBP, GUS i MF na pliki w data/

        Użycie:
          bonds-dane                        to samo co 'bonds-dane market'
          bonds-dane market [opcje]         serie rynkowe z plików w zrodla/nbp/
          bonds-dane letter PLIK.pdf        ustępy listu emisyjnego, po numerach

        Opcje komendy 'market':
          --data KATALOG          katalog danych kalkulatora, tu ląduje market/ (domyślnie data)
          --nbp-xml PLIK          archiwum stóp NBP (domyślnie zrodla/nbp/stopy_procentowe_archiwum.xml)
          --cpi-xlsx PLIK         arkusz NBP z inflacją bazową (domyślnie zrodla/nbp/bazowa.xlsx)
          --nbp-known-through RRRR-MM-DD
                                  ostatni dzień, dla którego stopa NBP jest znana;
                                  domyślnie dzisiaj albo dzień ostatniej decyzji,
                                  jeśli jest późniejszy
          --today RRRR-MM-DD      data uznawana za dzisiejszą (do testów)
          --dry-run               pokaż, co by się zmieniło, i nie zapisuj plików

        Opcje komendy 'letter':
          --fields                tylko ustępy, z których bierze się pola pliku emisji
          --annexes               same załączniki ze wzorami
          --paragraph N           wypisz ustęp numer N; można podać wielokrotnie

        Wspólne:
          -h, --help              ta pomoc

        Skrypt nie łączy się z siecią. Świeże pliki źródłowe pobiera człowiek
        do zrodla/nbp/:
          stopy_procentowe_archiwum.xml   https://nbp.pl/podstawowe-stopy-procentowe-archiwum/
          bazowa.xlsx                     https://nbp.pl/statystyka-i-sprawozdawczosc/inflacja-bazowa/
        Komenda 'letter' wymaga programu pdftotext z paczki poppler.

        Przykłady:
          bonds-dane --dry-run
          bonds-dane market --nbp-known-through 2026-03-05
          bonds-dane letter zrodla/listy_emisyjne/2026-09/file_409911.pdf --fields
        """;

    /// <summary>
    /// Reads the command line. <paramref name="today"/> is a parameter so that a
    /// test stays independent of the clock, and <paramref name="dataRoot"/> so that
    /// the tool works from any working directory.
    /// </summary>
    public static CommandLineOptions Parse(
        IReadOnlyList<string> arguments, DateOnly today, string dataRoot = "data")
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);

        // No command, or an option in the first place, means the market command.
        var command = SourcesCommand.Market;
        var firstOption = 0;
        if (arguments.Count > 0 && !arguments[0].StartsWith('-'))
        {
            command = ParseCommand(arguments[0]);
            firstOption = 1;
        }

        var data = dataRoot;
        string? nbp = null;
        string? cpi = null;
        DateOnly? knownThrough = null;
        var dryRun = false;
        string? letter = null;
        var fields = false;
        var annexes = false;
        var paragraphs = new List<int>();
        var help = false;

        for (var index = firstOption; index < arguments.Count; index++)
        {
            var name = arguments[index];
            switch (name)
            {
                case "-h" or "--help": help = true; break;
                case "--data": data = Value(arguments, ref index); break;
                case "--nbp-xml": nbp = Value(arguments, ref index); break;
                case "--cpi-xlsx": cpi = Value(arguments, ref index); break;
                case "--nbp-known-through":
                    knownThrough = ParseDate(Value(arguments, ref index), name);
                    break;
                case "--today": today = ParseDate(Value(arguments, ref index), name); break;
                case "--dry-run": dryRun = true; break;
                case "--fields": fields = true; break;
                case "--annexes": annexes = true; break;
                case "--paragraph": paragraphs.Add(ParseParagraph(Value(arguments, ref index))); break;
                default:
                    if (name.StartsWith('-'))
                    {
                        throw new CommandLineException($"Nieznana opcja '{name}'.");
                    }

                    if (letter is not null)
                    {
                        throw new CommandLineException(
                            $"Komenda 'letter' przyjmuje jeden plik; '{name}' jest drugim.");
                    }

                    letter = name;
                    break;
            }
        }

        var options = new CommandLineOptions(
            command,
            nbp ?? Path.Combine(SourcesDirectoryOf(data), NbpArchiveName),
            cpi ?? Path.Combine(SourcesDirectoryOf(data), CpiWorkbookName),
            Path.Combine(data, "market"),
            knownThrough,
            today,
            dryRun,
            letter,
            fields,
            annexes,
            paragraphs,
            help);

        // Help skips the checks below: "bonds-dane letter --help" must show the
        // help and not complain about a missing file.
        return help ? options : options.EnsureConsistent();
    }

    private CommandLineOptions EnsureConsistent()
    {
        if (Command == SourcesCommand.Letter && LetterPath is null)
        {
            throw new CommandLineException("Komenda 'letter' wymaga ścieżki do pliku PDF listu.");
        }

        if (Command == SourcesCommand.Market && LetterPath is not null)
        {
            throw new CommandLineException(
                $"Komenda 'market' nie przyjmuje ścieżki '{LetterPath}'. Może chodziło o 'letter'?");
        }

        if (FieldsOnly && AnnexesOnly)
        {
            throw new CommandLineException("Opcje --fields i --annexes wykluczają się.");
        }

        if (Command == SourcesCommand.Market && (FieldsOnly || AnnexesOnly || WantedParagraphs.Count > 0))
        {
            throw new CommandLineException(
                "Opcje --fields, --annexes i --paragraph działają tylko z komendą 'letter'.");
        }

        if (Command == SourcesCommand.Letter && DryRun)
        {
            throw new CommandLineException("Komenda 'letter' niczego nie zapisuje, więc --dry-run nic nie znaczy.");
        }

        if (NbpKnownThrough is not null && Command != SourcesCommand.Market)
        {
            throw new CommandLineException("Opcja --nbp-known-through działa tylko z komendą 'market'.");
        }

        return this;
    }

    /// <summary>
    /// The NBP source files are third-party documents, thus they live outside the
    /// data directory: in zrodla/nbp/, next to the data root. This keeps every
    /// file that the project licence does not cover in the one directory zrodla/.
    /// </summary>
    private static string SourcesDirectoryOf(string dataRoot) =>
        Path.Combine(Path.GetDirectoryName(Path.GetFullPath(dataRoot)) ?? dataRoot, "zrodla", "nbp");

    private static SourcesCommand ParseCommand(string text) => text switch
    {
        "market" => SourcesCommand.Market,
        "letter" => SourcesCommand.Letter,
        _ => throw new CommandLineException($"Nieznana komenda '{text}'. Dostępne: market, letter."),
    };

    private static int ParseParagraph(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 1
            ? value
            : throw new CommandLineException($"Opcja '--paragraph' wymaga numeru większego od zera; '{text}' nim nie jest.");
}
