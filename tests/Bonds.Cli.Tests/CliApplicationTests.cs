using Bonds.Cli.Tests.Support;
using Xunit;

namespace Bonds.Cli.Tests;

/// <summary>
/// The whole run, from raw arguments to the exit code. The writers are strings and
/// the data root is a parameter, thus no test touches the console or the clock.
/// </summary>
public sealed class CliApplicationTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 8, 17);

    private readonly StringWriter output = new();
    private readonly StringWriter error = new();
    private readonly string temporaryDirectory =
        Path.Combine(Path.GetTempPath(), "bonds-cli-tests-" + Guid.NewGuid().ToString("N"));

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

        Assert.Equal(CliApplication.Success, code);
        Assert.Contains("Użycie", output.ToString(), StringComparison.Ordinal);
        Assert.Empty(error.ToString());
    }

    [Fact]
    public void TheTableCommandPrintsMarkdownToTheOutput()
    {
        var code = Run("table", "--issue", "OTS1126");

        Assert.Equal(CliApplication.Success, code);
        Assert.Contains("# Polskie obligacje", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("OTS1126", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownOptionEndsWithAUserErrorAndTheUsageOnStderr()
    {
        var code = Run("--colour", "red");

        Assert.Equal(CliApplication.UserError, code);
        Assert.Empty(output.ToString());
        Assert.Contains("Błąd wywołania", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("Użycie", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingDataDirectoryEndsWithADataError()
    {
        var code = CliApplication.Run(
            ["table"], output, error, Today, Path.Combine(temporaryDirectory, "data"));

        Assert.Equal(CliApplication.DataError, code);
        Assert.Contains("Błąd danych", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownIssueCodeEndsWithAUserErrorWithoutParameterNoise()
    {
        var code = Run("table", "--issue", "XYZ9999");

        Assert.Equal(CliApplication.UserError, code);
        Assert.Contains("XYZ9999", error.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("(Parameter", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void APurchaseMonthWithNoIssuesListsTheMonthsThatExist()
    {
        var code = CliApplication.Run(
            ["table", "--purchase", "2026-12-01"], output, error, Today, RepositoryPaths.Data);

        Assert.Equal(CliApplication.DataError, code);
        Assert.Contains("2026-12", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("dostępne miesiące", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void APurchaseOutsideTheSaleWindowExplainsTheWindowInPolish()
    {
        var august = Path.Combine(RepositoryPaths.Data, "issues", "2026-08");

        var code = CliApplication.Run(
            ["table", "--issues", august, "--purchase", "2026-09-01"],
            output, error, Today, RepositoryPaths.Data);

        Assert.Equal(CliApplication.UserError, code);
        Assert.Contains("w sprzedaży od", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ThePurchaseDatePicksTheIssuesOfItsSaleMonth()
    {
        var code = CliApplication.Run(
            ["table", "--purchase", "2026-09-10", "--issue", "ROR0927"],
            output, error, Today, RepositoryPaths.Data);

        Assert.Equal(CliApplication.Success, code);
        Assert.Contains("ROR0927", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void OutWritesTheFileAndCreatesItsDirectories()
    {
        var target = Path.Combine(temporaryDirectory, "raporty", "ots.md");

        var code = Run("table", "--issue", "OTS1126", "--out", target);

        Assert.Equal(CliApplication.Success, code);
        Assert.True(File.Exists(target));
        Assert.Contains("Zapisano", error.ToString(), StringComparison.Ordinal);
        Assert.Empty(output.ToString());
    }

    [Fact]
    public void ACsvFileStartsWithAByteOrderMark()
    {
        // Excel on Windows reads a UTF-8 CSV as ANSI without the mark and mangles
        // the Polish diacritics.
        var target = Path.Combine(temporaryDirectory, "raport.csv");

        var code = Run("table", "--issue", "OTS1126", "--format", "csv", "--out", target);

        Assert.Equal(CliApplication.Success, code);
        var bytes = File.ReadAllBytes(target);
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes.Take(3));
    }

    [Fact]
    public void AnUnwritableOutPathEndsWithAUserErrorAndNoCrash()
    {
        // The parent of the target is a file, thus the directory cannot be made.
        Directory.CreateDirectory(temporaryDirectory);
        var blocker = Path.Combine(temporaryDirectory, "blocker");
        File.WriteAllText(blocker, "x");

        var code = Run("table", "--issue", "OTS1126", "--out", Path.Combine(blocker, "raport.md"));

        Assert.Equal(CliApplication.UserError, code);
        Assert.Contains("Błąd zapisu", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheMatrixCommandPrintsTheComparison()
    {
        var code = Run("matrix", "--metric", "profit");

        Assert.Equal(CliApplication.Success, code);
        Assert.Contains("# Porównanie", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheExplainCommandPrintsThePeriodsOfOneIssue()
    {
        var code = Run("explain", "--issue", "EDO0836");

        Assert.Equal(CliApplication.Success, code);
        Assert.Contains("przebieg naliczania", output.ToString(), StringComparison.Ordinal);
    }

    private int Run(params string[] arguments) =>
        CliApplication.Run(arguments, output, error, Today, RepositoryPaths.Data);
}
