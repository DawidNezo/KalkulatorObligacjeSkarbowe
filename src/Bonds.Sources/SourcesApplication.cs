using System.Globalization;
using Bonds.Core.CommandLine;
using Bonds.Sources.Letters;
using Bonds.Sources.Market;

namespace Bonds.Sources;

/// <summary>
/// The whole run of the tool, from the raw arguments to the exit code. It takes its
/// writers, its clock and its data root as parameters, thus a test can run it
/// without a console and without the real date.
/// </summary>
public static class SourcesApplication
{
    public const int Success = 0;

    /// <summary>The call was wrong: an unknown option or a bad value.</summary>
    public const int UserError = 1;

    /// <summary>A source file is missing, malformed or incomplete.</summary>
    public const int SourceError = 2;

    public static int Run(
        IReadOnlyList<string> arguments, TextWriter output, TextWriter error, DateOnly today, string dataRoot)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            var options = CommandLineOptions.Parse(arguments, today, dataRoot);
            if (options.ShowHelp)
            {
                output.WriteLine(CommandLineOptions.Usage);
                return Success;
            }

            return options.Command switch
            {
                SourcesCommand.Market => WriteMarketSeries(options, error),
                SourcesCommand.Letter => PrintParagraphs(options, output),
                _ => throw new CommandLineException($"Nieobsługiwana komenda '{options.Command}'."),
            };
        }
        catch (CommandLineException problem)
        {
            error.WriteLine($"Błąd wywołania: {problem.Message}");
            error.WriteLine();
            error.WriteLine(CommandLineOptions.Usage);
            return UserError;
        }
        catch (SourceException problem)
        {
            error.WriteLine($"Błąd danych źródłowych: {problem.Message}");
            return SourceError;
        }
        catch (IOException problem)
        {
            error.WriteLine($"Błąd zapisu: {problem.Message}");
            return UserError;
        }
        catch (UnauthorizedAccessException problem)
        {
            error.WriteLine($"Błąd zapisu: {problem.Message}");
            return UserError;
        }
    }

    private static int WriteMarketSeries(CommandLineOptions options, TextWriter error)
    {
        var rates = NbpReferenceArchive.Read(options.NbpArchivePath);
        var readings = CpiWorkbook.Read(options.CpiWorkbookPath);

        var lastDecision = rates.Keys.Max();
        var knownThrough = options.NbpKnownThrough
            ?? (options.Today > lastDecision ? options.Today : lastDecision);

        if (knownThrough < lastDecision)
        {
            throw new CommandLineException(
                $"--nbp-known-through {Day(knownThrough)} jest wcześniejsze niż ostatnia decyzja RPP "
                + $"w pliku ({Day(lastDecision)}).");
        }

        var written = new[]
        {
            Write(Path.Combine(options.MarketDirectory, "nbp-reference.json"),
                MarketSeriesWriter.RenderNbpReference(
                    rates, knownThrough, Path.GetFileName(options.NbpArchivePath)),
                options.DryRun),
            Write(Path.Combine(options.MarketDirectory, "cpi-yoy.json"),
                MarketSeriesWriter.RenderCpi(readings, Path.GetFileName(options.CpiWorkbookPath)),
                options.DryRun),
        };

        error.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"Stopa referencyjna NBP: {rates.Count} decyzji, ostatnia {Day(lastDecision)} = "
            + $"{Percent(rates[lastDecision])}%, dane znane do {Day(knownThrough)}."));

        // A "knownThrough" after the last decision means: we take it that the
        // Council decided nothing more. That holds for a fresh archive only. With
        // an old file the calculator would show a stale rate as data and not as a
        // projection, thus the assumption has to be visible.
        if (knownThrough > lastDecision && options.NbpKnownThrough is null)
        {
            error.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"    Założenie: archiwum jest aktualne na {Day(knownThrough)}, czyli po "
                + $"{Day(lastDecision)} nie było decyzji RPP."));
            error.WriteLine("    Jeśli plik jest stary, pobierz go jeszcze raz albo podaj "
                + "--nbp-known-through.");
        }

        var lastReading = readings.Keys.Max();
        error.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"Inflacja CPI r/r: {readings.Count} miesięcy, ostatni odczyt za {lastReading} = "
            + $"{Percent(readings[lastReading])}% (ogłoszony w "
            + $"{lastReading.AddMonths(MarketSeriesWriter.CpiPublicationLagMonths)})."));

        foreach (var (path, changed) in written)
        {
            var state = !changed ? "bez zmian" : options.DryRun ? "do zapisu" : "zapisano";
            error.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"  {Path.GetFileName(path),-24} {state}"));
        }

        return Success;
    }

    private static int PrintParagraphs(CommandLineOptions options, TextWriter output)
    {
        var path = options.LetterPath!;
        var letter = IssueLetter.Parse(PdfText.Of(path));

        output.WriteLine($"# {Path.GetFileName(path)}");
        foreach (var line in letter.Headline.Split('\n'))
        {
            output.WriteLine($"  {line}");
        }

        output.WriteLine();

        if (options.AnnexesOnly)
        {
            output.WriteLine(letter.Annexes.Length == 0
                ? "Ten list nie ma załączników."
                : letter.Annexes.TrimEnd());
            return Success;
        }

        if (options.WantedParagraphs.Count > 0)
        {
            foreach (var number in options.WantedParagraphs)
            {
                var paragraph = letter.Paragraph(number);
                output.WriteLine(paragraph is null
                    ? $"[{number}] NIE MA takiego ustępu; list ma {letter.Paragraphs.Length}."
                    : $"[{paragraph.Value.Number}] {paragraph.Value.Text}");
                output.WriteLine();
            }

            return Success;
        }

        if (options.FieldsOnly)
        {
            foreach (var hint in IssueFieldHints.For(letter))
            {
                output.WriteLine($"## {hint.Field}");
                if (hint.Paragraphs.IsEmpty)
                {
                    output.WriteLine("  NIE ZNALEZIONO — przeczytaj list ręcznie.");
                }

                foreach (var paragraph in hint.Paragraphs)
                {
                    output.WriteLine($"  [{paragraph.Number}] {paragraph.Text}");
                }

                output.WriteLine();
            }

            return Success;
        }

        foreach (var paragraph in letter.Paragraphs)
        {
            output.WriteLine($"[{paragraph.Number}] {paragraph.Text}");
            output.WriteLine();
        }

        return Success;
    }

    /// <summary>
    /// Writes a file and says whether its content changed. An unchanged file is not
    /// touched, so that a re-run leaves no trace when the sources are the same.
    /// </summary>
    private static (string Path, bool Changed) Write(string path, string text, bool dryRun)
    {
        var unchanged = File.Exists(path)
            && string.Equals(File.ReadAllText(path), text, StringComparison.Ordinal);

        if (!unchanged && !dryRun)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, text);
        }

        return (path, !unchanged);
    }

    private static string Day(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Percent(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);
}
