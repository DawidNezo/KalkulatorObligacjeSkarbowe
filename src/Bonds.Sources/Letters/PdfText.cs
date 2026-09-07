using System.Diagnostics;

namespace Bonds.Sources.Letters;

/// <summary>
/// Turns a PDF into text with "pdftotext" of poppler.
/// <para>
/// This is the one step that needs a program outside .NET. A PDF reader written
/// here would be a large piece of code for one command that a person runs eight
/// times a month, and no package may enter this repository. The option "-layout"
/// keeps the columns of a table readable.
/// </para>
/// </summary>
public static class PdfText
{
    public const string Program = "pdftotext";

    private const string LayoutOption = "-layout";

    /// <summary>Writes to standard output instead of a file.</summary>
    private const string StandardOutput = "-";

    public static string Of(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new SourceException(path, "plik nie istnieje");
        }

        var start = new ProcessStartInfo(Program)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add(LayoutOption);
        start.ArgumentList.Add(path);
        start.ArgumentList.Add(StandardOutput);

        Process process;
        try
        {
            process = Process.Start(start)
                ?? throw new SourceException(path, $"nie udało się uruchomić {Program}");
        }
        catch (System.ComponentModel.Win32Exception error)
        {
            throw new SourceException(path,
                $"brak programu {Program}. Zainstaluj poppler: brew install poppler", error);
        }

        using (process)
        {
            // The two pipes drain concurrently. A reader that empties one pipe to
            // its end while the process still fills the other would deadlock as
            // soon as the other pipe buffer runs full.
            var problemsTask = process.StandardError.ReadToEndAsync();
            var text = process.StandardOutput.ReadToEnd();
            var problems = problemsTask.GetAwaiter().GetResult();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new SourceException(path,
                    $"{Program} zakończył się kodem {process.ExitCode}: {problems.Trim()}");
            }

            return text;
        }
    }
}
