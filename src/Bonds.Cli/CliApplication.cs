using System.Text;
using Bonds.Core;
using Bonds.Core.Application;
using Bonds.Core.Calendar;
using Bonds.Core.CommandLine;
using Bonds.Core.Data;
using Bonds.Core.Engine;
using Bonds.Core.Reporting;

namespace Bonds.Cli;

/// <summary>
/// The whole run of the tool, from the raw arguments to the exit code. It takes
/// its writers, its clock and its data root as parameters, thus a test can run it
/// without a console, without the real date and without the repository.
/// </summary>
public static class CliApplication
{
    public const int Success = 0;

    /// <summary>The call was wrong: an unknown option, a bad value, a missing issue code.</summary>
    public const int UserError = 1;

    /// <summary>A data file is missing, malformed or incomplete.</summary>
    public const int DataError = 2;

    /// <summary>Excel on Windows reads a UTF-8 CSV correctly only when it starts with a BOM.</summary>
    private static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

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

            var calculator = new BondCalculator(new JsonDataStore(), PolishBusinessDayCalendar.Instance);
            var report = calculator.Run(new CalculationRequest(
                options.IssuesDirectory,
                options.ScenarioPath,
                options.NbpSeriesPath,
                options.CpiSeriesPath,
                options.Purchase,
                options.Units,
                options.TaxRate,
                options.IssueCodes));

            return Deliver(options, Render(options, report), output, error);
        }
        catch (CommandLineException problem)
        {
            error.WriteLine($"Błąd wywołania: {problem.Message}");
            error.WriteLine();
            error.WriteLine(CommandLineOptions.Usage);
            return UserError;
        }
        catch (DataLoadException problem)
        {
            error.WriteLine($"Błąd danych: {problem.Message}");
            return DataError;
        }
        catch (KeyNotFoundException problem)
        {
            error.WriteLine($"Błąd danych: {problem.Message}");
            return DataError;
        }
        catch (ArgumentException problem)
        {
            error.WriteLine($"Błąd: {ErrorMessages.WithoutParameterName(problem.Message)}");
            return UserError;
        }
    }

    private static int Deliver(CommandLineOptions options, string text, TextWriter output, TextWriter error)
    {
        if (options.OutputPath is null)
        {
            output.Write(text);
            return Success;
        }

        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(options.OutputPath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var encoding = options.OutputFormat == "csv" ? Utf8WithBom : Utf8NoBom;
            File.WriteAllText(options.OutputPath, text, encoding);
            error.WriteLine($"Zapisano {options.OutputPath}");
            return Success;
        }
        catch (Exception problem)
            when (problem is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error.WriteLine($"Błąd zapisu: {problem.Message}");
            return UserError;
        }
    }

    private static string Render(CommandLineOptions options, CalculationReport report) => options.Command switch
    {
        CliCommand.Table => Writer(options.OutputFormat).Render(report),
        CliCommand.Matrix => new ComparisonMatrixWriter(options.Metric).Render(report),
        CliCommand.Explain => new ExplainWriter(new RedemptionCalculator()).Render(report, options.IssueCodes[0]),
        _ => throw new CommandLineException($"Nieobsługiwana komenda '{options.Command}'."),
    };

    private static IReportWriter Writer(string format) => format switch
    {
        "md" => new MarkdownReportWriter(),
        "csv" => new CsvReportWriter(),
        "json" => new JsonReportWriter(),
        _ => throw new CommandLineException($"Nieznany format '{format}'."),
    };
}
