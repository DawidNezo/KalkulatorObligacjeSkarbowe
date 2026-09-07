using Bonds.Sources.Tests.Support;
using Xunit;

namespace Bonds.Sources.Tests;

/// <summary>
/// The whole run, from raw arguments to the exit code. The writers are strings and
/// the data root is a parameter, thus no test touches the console or the clock.
/// </summary>
public sealed class SourcesApplicationTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 8, 28);

    private readonly StringWriter output = new();
    private readonly StringWriter error = new();
    private readonly string temporaryDirectory =
        Path.Combine(Path.GetTempPath(), "bonds-sources-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        output.Dispose();
        error.Dispose();
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public void HelpEndsWithSuccessAndPrintsTheUsage()
    {
        var code = Run("--help");

        Assert.Equal(SourcesApplication.Success, code);
        Assert.Contains("Użycie", output.ToString(), StringComparison.Ordinal);
        Assert.Empty(error.ToString());
    }

    [Fact]
    public void AnUnknownOptionEndsWithAUserErrorAndTheUsageOnStderr()
    {
        var code = Run("--kolor", "czerwony");

        Assert.Equal(SourcesApplication.UserError, code);
        Assert.Contains("Błąd wywołania", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("Użycie", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownCommandNamesTheOnesThatExist()
    {
        var code = Run("inflacja");

        Assert.Equal(SourcesApplication.UserError, code);
        Assert.Contains("market, letter", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingSourceFileEndsWithASourceError()
    {
        var code = SourcesApplication.Run(
            [], output, error, Today, Path.Combine(temporaryDirectory, "data"));

        Assert.Equal(SourcesApplication.SourceError, code);
        Assert.Contains("Błąd danych źródłowych", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("nie istnieje", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void WritesBothSeriesFromTheShippedSources()
    {
        var code = Run("--data", Target());

        Assert.Equal(SourcesApplication.Success, code);
        Assert.True(File.Exists(Path.Combine(Target(), "market", "nbp-reference.json")));
        Assert.True(File.Exists(Path.Combine(Target(), "market", "cpi-yoy.json")));
        Assert.Contains("zapisano", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ADryRunWritesNothing()
    {
        var code = Run("--data", Target(), "--dry-run");

        Assert.Equal(SourcesApplication.Success, code);
        Assert.False(File.Exists(Path.Combine(Target(), "market", "cpi-yoy.json")));
        Assert.Contains("do zapisu", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ARerunWithTheSameSourcesChangesNothing()
    {
        Run("--data", Target());
        var written = File.GetLastWriteTimeUtc(Path.Combine(Target(), "market", "cpi-yoy.json"));

        using var second = new StringWriter();
        var code = SourcesApplication.Run(
            ["--data", Target()], output, second, Today, RepositoryPaths.Data);

        Assert.Equal(SourcesApplication.Success, code);
        Assert.Contains("bez zmian", second.ToString(), StringComparison.Ordinal);
        Assert.Equal(written, File.GetLastWriteTimeUtc(Path.Combine(Target(), "market", "cpi-yoy.json")));
    }

    [Fact]
    public void SaysThatItTakesTheArchiveForCurrent()
    {
        // "knownThrough" of today, with the last decision months back, is an
        // assumption about a file that nobody checked. It must be stated.
        var code = Run("--data", Target());

        Assert.Equal(SourcesApplication.Success, code);
        Assert.Contains("Założenie: archiwum jest aktualne", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SaysNothingAboutTheAssumptionWhenTheDayIsGiven()
    {
        var rates = Sources.Market.NbpReferenceArchive.Read(RepositoryPaths.NbpArchive);

        var code = Run("--data", Target(),
            "--nbp-known-through", rates.Keys.Max().ToString("yyyy-MM-dd", provider: null));

        Assert.Equal(SourcesApplication.Success, code);
        Assert.DoesNotContain("Założenie", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void RefusesADayBeforeTheLastDecision()
    {
        var code = Run("--data", Target(), "--nbp-known-through", "2000-01-01");

        Assert.Equal(SourcesApplication.UserError, code);
        Assert.Contains("wcześniejsze niż ostatnia decyzja", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void RefusesAnOptionOfTheOtherCommand()
    {
        var code = Run("market", "--fields");

        Assert.Equal(SourcesApplication.UserError, code);
        Assert.Contains("tylko z komendą 'letter'", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheParagraphCommandNeedsAFile()
    {
        var code = Run("letter");

        Assert.Equal(SourcesApplication.UserError, code);
        Assert.Contains("wymaga ścieżki", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsAMissingLetter()
    {
        var code = Run("letter", Path.Combine(temporaryDirectory, "nie-ma.pdf"));

        Assert.Equal(SourcesApplication.SourceError, code);
        Assert.Contains("nie istnieje", error.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// The data root that a test writes into. The layout mirrors the repository:
    /// the NBP sources sit in zrodla/nbp/ next to data/, thus the default source
    /// paths of the tool resolve, and the market directory is fresh.
    /// </summary>
    private string Target()
    {
        var data = Path.Combine(temporaryDirectory, "data");
        if (!Directory.Exists(data))
        {
            Directory.CreateDirectory(data);
            var sources = Path.Combine(temporaryDirectory, "zrodla", "nbp");
            Directory.CreateDirectory(sources);
            File.Copy(RepositoryPaths.NbpArchive,
                Path.Combine(sources, CommandLineOptions.NbpArchiveName));
            File.Copy(RepositoryPaths.CpiWorkbook,
                Path.Combine(sources, CommandLineOptions.CpiWorkbookName));
        }

        return data;
    }

    private int Run(params string[] arguments) =>
        SourcesApplication.Run(arguments, output, error, Today, RepositoryPaths.Data);
}
