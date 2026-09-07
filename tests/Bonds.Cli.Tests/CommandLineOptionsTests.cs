using Bonds.Core.CommandLine;
using Bonds.Core.Reporting;
using Xunit;

namespace Bonds.Cli.Tests;

public sealed class CommandLineOptionsTests
{
    private static readonly DateOnly Today = new(2026, 8, 27);

    [Fact]
    public void ReadsTheCommandAndTheRequiredDate()
    {
        var options = CommandLineOptions.Parse(["table", "--purchase", "2026-08-17"], Today);

        Assert.Equal(CliCommand.Table, options.Command);
        Assert.Equal(new DateOnly(2026, 8, 17), options.Purchase);
    }

    [Fact]
    public void UsesTheDocumentedDefaults()
    {
        var options = CommandLineOptions.Parse(["table", "--purchase", "2026-08-17"], Today);

        Assert.Equal(1, options.Units);
        Assert.Equal(19m, options.TaxRatePercent);
        Assert.Equal(0.19m, options.TaxRate);
        Assert.Equal("md", options.OutputFormat);
        Assert.Equal(ExitMetric.AnnualisedNetReturn, options.Metric);
        Assert.Null(options.OutputPath);
        Assert.Empty(options.IssueCodes);
        Assert.False(options.ShowHelp);
    }

    [Fact]
    public void GrowsTheDefaultPathsFromTheDataRoot()
    {
        var options = CommandLineOptions.Parse([], Today, dataRoot: Path.Combine("repo", "data"));

        Assert.Equal(Path.Combine("repo", "data", "issues"), options.IssuesDirectory);
        Assert.Equal(Path.Combine("repo", "data", "scenarios", "base.json"), options.ScenarioPath);
        Assert.Equal(Path.Combine("repo", "data", "market", "nbp-reference.json"), options.NbpSeriesPath);
        Assert.Equal(Path.Combine("repo", "data", "market", "cpi-yoy.json"), options.CpiSeriesPath);
    }

    [Fact]
    public void ReadsSeveralIssueCodes()
    {
        var options = CommandLineOptions.Parse(
            ["table", "--purchase", "2026-08-17", "--issue", "ROR0827,DOR0828", "--issue", "EDO0836"], Today);

        Assert.Equal(["ROR0827", "DOR0828", "EDO0836"], options.IssueCodes);
    }

    [Fact]
    public void UsesTodayWhenNoPurchaseDateIsGiven() =>
        Assert.Equal(Today, CommandLineOptions.Parse(["table"], Today).Purchase);

    [Fact]
    public void NoArgumentsMeansTheTableCommandForToday()
    {
        var options = CommandLineOptions.Parse([], Today);

        Assert.Equal(CliCommand.Table, options.Command);
        Assert.Equal(Today, options.Purchase);
    }

    [Fact]
    public void AnOptionInTheFirstPlaceMeansTheTableCommand()
    {
        var options = CommandLineOptions.Parse(["--units", "1000"], Today);

        Assert.Equal(CliCommand.Table, options.Command);
        Assert.Equal(1000, options.Units);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    public void ReadsTheHelpFlag(string flag) =>
        Assert.True(CommandLineOptions.Parse([flag], Today).ShowHelp);

    [Fact]
    public void HelpSkipsTheConsistencyChecks() =>
        // "explain --help" must show the help and not complain about --issue.
        Assert.True(CommandLineOptions.Parse(["explain", "--help"], Today).ShowHelp);

    [Fact]
    public void AnOptionCannotSwallowTheHelpFlagAsItsValue() =>
        Assert.Throws<CommandLineException>(() => CommandLineOptions.Parse(["table", "--out", "--help"], Today));

    [Theory]
    [InlineData("17-08-2026")]
    [InlineData("2026/08/17")]
    [InlineData("tomorrow")]
    public void RejectsADateInTheWrongFormat(string date) =>
        Assert.Throws<CommandLineException>(() => CommandLineOptions.Parse(["table", "--purchase", date], Today));

    [Fact]
    public void RejectsAnUnknownCommand() =>
        Assert.Throws<CommandLineException>(() => CommandLineOptions.Parse(["chart", "--purchase", "2026-08-17"], Today));

    [Fact]
    public void RejectsAnUnknownOption() =>
        Assert.Throws<CommandLineException>(
            () => CommandLineOptions.Parse(["table", "--purchase", "2026-08-17", "--colour", "red"], Today));

    [Fact]
    public void RejectsAnOptionWithNoValue() =>
        Assert.Throws<CommandLineException>(() => CommandLineOptions.Parse(["table", "--purchase"], Today));

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("many")]
    public void RejectsAHoldingThatIsNotAWholeNumberAboveZero(string units) =>
        Assert.Throws<CommandLineException>(
            () => CommandLineOptions.Parse(["table", "--purchase", "2026-08-17", "--units", units], Today));

    [Fact]
    public void RejectsATaxThatIsNotANumber() =>
        Assert.Throws<CommandLineException>(
            () => CommandLineOptions.Parse(["table", "--tax", "duzo"], Today));

    [Fact]
    public void ReadsANegativeNumberAsAValue() =>
        // "-5" is a value and not an option; the range check happens later.
        Assert.Equal(-5m, CommandLineOptions.Parse(["table", "--tax", "-5"], Today).TaxRatePercent);

    [Fact]
    public void RejectsAnUnknownFormat() =>
        Assert.Throws<CommandLineException>(
            () => CommandLineOptions.Parse(["table", "--purchase", "2026-08-17", "--format", "pdf"], Today));

    [Fact]
    public void ReadsTheFormatRegardlessOfTheCase() =>
        Assert.Equal("csv", CommandLineOptions.Parse(["table", "--format", "CSV"], Today).OutputFormat);

    [Theory]
    [InlineData("profit", ExitMetric.NetProfit)]
    [InlineData("rate", ExitMetric.NetProfitRate)]
    [InlineData("irr", ExitMetric.AnnualisedNetReturn)]
    public void ReadsEveryMetric(string name, ExitMetric expected) =>
        Assert.Equal(expected, CommandLineOptions.Parse(["matrix", "--metric", name], Today).Metric);

    [Fact]
    public void RejectsAnUnknownMetric() =>
        Assert.Throws<CommandLineException>(
            () => CommandLineOptions.Parse(["matrix", "--purchase", "2026-08-17", "--metric", "alpha"], Today));

    [Fact]
    public void RejectsTheFormatOptionOutsideTheTableCommand() =>
        Assert.Throws<CommandLineException>(
            () => CommandLineOptions.Parse(["matrix", "--format", "json"], Today));

    [Fact]
    public void RejectsTheMetricOptionOutsideTheMatrixCommand() =>
        Assert.Throws<CommandLineException>(
            () => CommandLineOptions.Parse(["table", "--metric", "profit"], Today));

    [Fact]
    public void ExplainNeedsExactlyOneIssue()
    {
        Assert.Throws<CommandLineException>(
            () => CommandLineOptions.Parse(["explain", "--purchase", "2026-08-17"], Today));
        Assert.Throws<CommandLineException>(
            () => CommandLineOptions.Parse(["explain", "--purchase", "2026-08-17", "--issue", "A,B"], Today));

        var options = CommandLineOptions.Parse(
            ["explain", "--purchase", "2026-08-17", "--issue", "EDO0836"], Today);
        Assert.Equal(CliCommand.Explain, options.Command);
    }

    [Fact]
    public void ReadsEveryPathOption()
    {
        var options = CommandLineOptions.Parse([
            "table", "--purchase", "2026-08-17",
            "--issues", "a", "--scenario", "b", "--nbp", "c", "--cpi", "d", "--out", "e",
        ], Today);

        Assert.Equal("a", options.IssuesDirectory);
        Assert.Equal("b", options.ScenarioPath);
        Assert.Equal("c", options.NbpSeriesPath);
        Assert.Equal("d", options.CpiSeriesPath);
        Assert.Equal("e", options.OutputPath);
    }

    [Fact]
    public void UsageNamesEveryCommand()
    {
        Assert.Contains("table", CommandLineOptions.Usage, StringComparison.Ordinal);
        Assert.Contains("matrix", CommandLineOptions.Usage, StringComparison.Ordinal);
        Assert.Contains("explain", CommandLineOptions.Usage, StringComparison.Ordinal);
    }
}
