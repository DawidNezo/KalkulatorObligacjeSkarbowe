using Bonds.Core.Application;
using Bonds.Core.Calendar;
using Bonds.Core.Data;
using Bonds.Core.Engine;
using Bonds.Core.Tests.Support;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class BondCalculatorTests
{
    private readonly BondCalculator calculator =
        new(new JsonDataStore(), PolishBusinessDayCalendar.Instance);

    [Fact]
    public void RunsEveryShippedIssueEndToEnd()
    {
        var report = calculator.Run(Request());

        Assert.Equal(8, report.Bonds.Length);
        Assert.All(report.Bonds, bond => Assert.NotEmpty(bond.Exits));
    }

    [Fact]
    public void EveryTableEndsWithAMonthThatDoesNotExist()
    {
        var report = calculator.Run(Request());

        Assert.All(report.Bonds, bond =>
        {
            Assert.Equal(ExitKind.DoesNotExist, bond.Exits[^1].Kind);
            Assert.Equal(bond.Plan.Issue.TermMonths + 1, bond.Exits[^1].MonthIndex);
        });
    }

    [Fact]
    public void SelectsOnlyTheIssuesAsked()
    {
        var report = calculator.Run(Request() with { OnlyCodes = ["EDO0836", "ror0827"] });

        Assert.Equal(["EDO0836", "ROR0827"], report.Bonds.Select(b => b.Plan.Issue.Code));
    }

    [Fact]
    public void ReportsACodeThatNoFileHolds()
    {
        var error = Assert.Throws<ArgumentException>(
            () => calculator.Run(Request() with { OnlyCodes = ["EDO0836", "NOPE1234"] }));

        Assert.Contains("NOPE1234", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MarksTheShippedMarketDataAsAProjection()
    {
        // The repository ships placeholder market series, thus every rate after the
        // first period must show up as a projection.
        var report = calculator.Run(Request() with { OnlyCodes = ["EDO0836"] });
        var plan = report.Bonds[0].Plan;

        Assert.False(plan.Periods[0].IsProjected);
        Assert.NotNull(plan.FirstProjectedPeriod);
        Assert.Equal(1, plan.FirstProjectedPeriod.Period.Index);
    }

    [Fact]
    public void ReadsTheIssuesOfTheMonthThatThePurchaseFallsIn()
    {
        var report = calculator.Run(Request() with { Purchase = new DateOnly(2026, 9, 15) });

        Assert.Equal(
            ["COI0930", "DOR0928", "EDO0936", "OTS1226", "ROD0938", "ROR0927", "ROS0932", "TOS0929"],
            report.Bonds.Select(b => b.Plan.Issue.Code));
    }

    [Fact]
    public void ReportsAPurchaseMonthThatNoIssueDirectoryCovers()
    {
        var error = Assert.Throws<DataLoadException>(
            () => calculator.Run(Request() with { Purchase = new DateOnly(2026, 12, 1) }));

        Assert.Contains("2026-12", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAPurchaseOutsideTheSaleWindowOfTheIssuesItWasGiven() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Run(Request() with
        {
            IssuesDirectory = RepositoryPaths.IssuesFor("2026-08"),
            Purchase = new DateOnly(2026, 9, 1),
        }));

    [Fact]
    public void RejectsAHoldingOfLessThanOneBond() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Run(Request() with { Units = 0 }));

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.5)]
    public void RejectsATaxRateOutsideZeroToOne(decimal rate) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Run(Request() with { TaxRate = rate }));

    [Fact]
    public void UsesTheAssumptionsOfTheScenarioThatItIsGiven()
    {
        var high = Path.Combine(RepositoryPaths.Data, "scenarios", "wysoka-inflacja.json");

        var baseline = calculator.Run(Request() with { OnlyCodes = ["EDO0836"] });
        var shock = calculator.Run(Request() with { OnlyCodes = ["EDO0836"], ScenarioPath = high });

        var baselineFinal = baseline.Bonds[0].Exits.Single(e => e.Kind == ExitKind.Maturity).Details!;
        var shockFinal = shock.Bonds[0].Exits.Single(e => e.Kind == ExitKind.Maturity).Details!;

        Assert.True(shockFinal.RedemptionGross > baselineFinal.RedemptionGross);
    }

    private static CalculationRequest Request() => new(
        RepositoryPaths.Issues,
        Path.Combine(RepositoryPaths.Data, "scenarios", "base.json"),
        Path.Combine(RepositoryPaths.Data, "market", "nbp-reference.json"),
        Path.Combine(RepositoryPaths.Data, "market", "cpi-yoy.json"),
        TestIssues.Purchase,
        Units: 1,
        TaxRate: TaxCalculator.StandardRate,
        OnlyCodes: []);
}
