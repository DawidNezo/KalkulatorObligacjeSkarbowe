using Bonds.Core.Data;
using Bonds.Core.Domain;
using Bonds.Core.Tests.Support;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class JsonDataStoreTests : IDisposable
{
    private readonly JsonDataStore store = new();
    private readonly string temporaryDirectory =
        Path.Combine(Path.GetTempPath(), "bonds-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    /// <summary>
    /// The sale month whose issue files this test class reads. Every month ships
    /// the same eight kinds, thus one month is enough to check the fields.
    /// ShippedIssueMonthsTests covers every other month.
    /// </summary>
    private static string August => RepositoryPaths.IssuesFor("2026-08");

    [Fact]
    public void LoadsTheEightShippedIssues()
    {
        var issues = store.LoadIssues(August);

        Assert.Equal(8, issues.Count);
        Assert.Equal(
            ["COI0830", "DOR0828", "EDO0836", "OTS1126", "ROD0838", "ROR0827", "ROS0832", "TOS0829"],
            issues.Select(i => i.Code));
    }

    [Fact]
    public void EveryShippedIssueNamesItsIssueLetter() =>
        Assert.All(store.LoadIssues(August),
            issue => Assert.Contains("List emisyjny", issue.Source, StringComparison.Ordinal));

    [Theory]
    [InlineData("OTS1126", 4, 1, false, "None")]
    [InlineData("ROR0827", 12, 12, false, "FirstPeriodOnly")]
    [InlineData("DOR0828", 12, 24, false, "FirstPeriodOnly")]
    [InlineData("TOS0829", 1, 3, true, "AllPeriods")]
    [InlineData("COI0830", 1, 4, false, "FirstPeriodOnly")]
    [InlineData("ROS0832", 1, 6, true, "AllPeriods")]
    [InlineData("EDO0836", 1, 10, true, "AllPeriods")]
    [InlineData("ROD0838", 1, 12, true, "AllPeriods")]
    public void ShippedIssuesCarryTheParametersOfTheirLetters(
        string code, int periodsPerYear, int periodCount, bool capitalizes, string floor)
    {
        var issue = store.LoadIssues(August).Single(i => i.Code == code);

        Assert.Equal(periodsPerYear, issue.PeriodsPerYear);
        Assert.Equal(periodCount, issue.PeriodCount);
        Assert.Equal(capitalizes, issue.Capitalizes);
        Assert.Equal(Enum.Parse<PrincipalFloor>(floor), issue.Floor);
        Assert.Equal(Money.RoundToGrosz(100m), issue.Nominal);
    }

    [Fact]
    public void OnlyTosRoundsItsCapitalisedBase()
    {
        var issues = store.LoadIssues(August);

        Assert.Equal(["TOS0829"], issues.Where(i => i.RoundCapitalizedBase).Select(i => i.Code));
    }

    [Fact]
    public void ReadsPercentValuesAsFractions()
    {
        var issue = store.LoadIssues(August).Single(i => i.Code == "EDO0836");
        var rate = Assert.IsType<InflationLinkedRate>(issue.Rate);

        Assert.Equal(0.0535m, rate.FirstRate);
        Assert.Equal(0.0200m, rate.Margin);
    }

    [Fact]
    public void LoadsTheShippedScenario()
    {
        var scenario = store.LoadScenario(Path.Combine(RepositoryPaths.Data, "scenarios", "base.json"));

        Assert.Equal(0.0250m, scenario.AssumedCpi);
        Assert.Equal(0.0375m, scenario.AssumedNbpReference);
        Assert.False(string.IsNullOrWhiteSpace(scenario.Name));
    }

    [Fact]
    public void LoadsTheShippedMarketSeriesAndSkipsComments()
    {
        var nbp = store.LoadDatedSeries(Path.Combine(RepositoryPaths.Data, "market", "nbp-reference.json"));
        var cpi = store.LoadMonthlySeries(Path.Combine(RepositoryPaths.Data, "market", "cpi-yoy.json"));

        Assert.NotNull(nbp.ValueOn(nbp.EarliestDate));
        Assert.NotNull(cpi.ValueIn(cpi.KnownThrough));
    }

    [Fact]
    public void ReportsAMissingFile()
    {
        var error = Assert.Throws<DataLoadException>(
            () => store.LoadScenario(Path.Combine(temporaryDirectory, "nothing.json")));

        Assert.Contains("nie istnieje", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsAMissingDirectory() =>
        Assert.Throws<DataLoadException>(() => store.LoadIssues(Path.Combine(temporaryDirectory, "issues")));

    [Fact]
    public void ReportsAnEmptyDirectory()
    {
        Directory.CreateDirectory(temporaryDirectory);

        var error = Assert.Throws<DataLoadException>(() => store.LoadIssues(temporaryDirectory));

        Assert.Contains("żadnego pliku *.json", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsTheNameOfAMissingFieldAndTheFile()
    {
        var path = WriteTemporary("broken.json", """{ "code": "X", "kind": "Coi" }""");

        var error = Assert.Throws<DataLoadException>(() => store.LoadIssue(path));

        Assert.Contains("brak pola", error.Message, StringComparison.Ordinal);
        Assert.Contains("Sale", error.Message, StringComparison.Ordinal);
        Assert.Equal(path, error.Path);
    }

    [Fact]
    public void ReportsAMissingSource()
    {
        var complete = IssueWith("""
            "rate": { "type": "inflation", "firstPeriodRatePercent": 5.0, "marginPercent": 1.0 },
            "fee": { "type": "twoTier", "amountZloty": 2.0 }
            """);
        var path = WriteTemporary("issue.json",
            complete.Replace("\"source\": \"test\",", string.Empty, StringComparison.Ordinal));

        var error = Assert.Throws<DataLoadException>(() => store.LoadIssue(path));

        Assert.Contains("Source", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsBrokenJson()
    {
        var path = WriteTemporary("broken.json", "{ this is not json");

        var error = Assert.Throws<DataLoadException>(() => store.LoadIssue(path));

        Assert.Contains("nie można odczytać pliku", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsAFileThatHoldsNoObject()
    {
        var path = WriteTemporary("null.json", "null");

        var error = Assert.Throws<DataLoadException>(() => store.LoadIssue(path));

        Assert.Contains("nie zawiera obiektu", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsAFieldThatNoDocumentDeclares()
    {
        // A typo like "kownThrough" must fail loudly instead of loading defaults.
        var path = WriteTemporary("cpi.json", """
            { "name": "x", "kownThrough": "2026-01", "percent": { "2026-01": 3.0 } }
            """);

        Assert.Throws<DataLoadException>(() => store.LoadMonthlySeries(path));
    }

    [Fact]
    public void ReportsAnUnknownRateType()
    {
        var path = WriteTemporary("issue.json", IssueWith("""
            "rate": { "type": "magic", "firstPeriodRatePercent": 5.0, "marginPercent": 1.0 },
            "fee": { "type": "twoTier", "amountZloty": 2.0 }
            """));

        var error = Assert.Throws<DataLoadException>(() => store.LoadIssue(path));

        Assert.Contains("typem oprocentowania", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsAnUnknownFeeType()
    {
        var path = WriteTemporary("issue.json", IssueWith("""
            "rate": { "type": "inflation", "firstPeriodRatePercent": 5.0, "marginPercent": 1.0 },
            "fee": { "type": "magic", "amountZloty": 2.0 }
            """));

        var error = Assert.Throws<DataLoadException>(() => store.LoadIssue(path));

        Assert.Contains("typem opłaty", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsAnUnknownBondKind()
    {
        var path = WriteTemporary("issue.json", IssueWith("""
            "rate": { "type": "inflation", "firstPeriodRatePercent": 5.0, "marginPercent": 1.0 },
            "fee": { "type": "twoTier", "amountZloty": 2.0 }
            """).Replace("\"kind\": \"Coi\"", "\"kind\": \"XYZ\"", StringComparison.Ordinal));

        var error = Assert.Throws<DataLoadException>(() => store.LoadIssue(path));

        Assert.Contains("poprawną wartością BondKind", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsAMissingRoundCapitalizedBase()
    {
        // A silent default of "false" would change the capitalisation of TOS,
        // thus the field is required like every other one.
        var complete = IssueWith("""
            "rate": { "type": "inflation", "firstPeriodRatePercent": 5.0, "marginPercent": 1.0 },
            "fee": { "type": "twoTier", "amountZloty": 2.0 }
            """);
        var path = WriteTemporary("issue.json",
            complete.Replace("\"roundCapitalizedBase\": false,", string.Empty, StringComparison.Ordinal));

        var error = Assert.Throws<DataLoadException>(() => store.LoadIssue(path));

        Assert.Contains("RoundCapitalizedBase", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WrapsAValidationErrorWithTheNameOfTheFile()
    {
        var complete = IssueWith("""
            "rate": { "type": "inflation", "firstPeriodRatePercent": 5.0, "marginPercent": 1.0 },
            "fee": { "type": "twoTier", "amountZloty": 2.0 }
            """);
        var path = WriteTemporary("issue.json",
            complete.Replace("\"purchasePriceZloty\": 100.00", "\"purchasePriceZloty\": 99.90",
                StringComparison.Ordinal));

        var error = Assert.Throws<DataLoadException>(() => store.LoadIssue(path));

        Assert.Equal(path, error.Path);
        Assert.Contains("dyskonta", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("(Parameter", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsACodeThatTwoFilesShare()
    {
        var issue = IssueWith("""
            "rate": { "type": "inflation", "firstPeriodRatePercent": 5.0, "marginPercent": 1.0 },
            "fee": { "type": "twoTier", "amountZloty": 2.0 }
            """);
        WriteTemporary("a.json", issue);
        WriteTemporary("b.json", issue);

        var error = Assert.Throws<DataLoadException>(() => store.LoadIssues(temporaryDirectory));

        Assert.Contains("TEST0101", error.Message, StringComparison.Ordinal);
        Assert.Contains("powtarza się", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsADuplicatedKeyInASeries()
    {
        var path = WriteTemporary("cpi.json", """
            { "name": "x", "knownThrough": "2026-02",
              "percent": { "2026-01": 3.0, "2026-02": 3.1, "2026-02": 9.9 } }
            """);

        var error = Assert.Throws<DataLoadException>(() => store.LoadMonthlySeries(path));

        Assert.Contains("2026-02", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsAGapInAMonthlySeries()
    {
        var path = WriteTemporary("cpi.json", """
            { "name": "x", "knownThrough": "2026-03",
              "percent": { "2026-01": 3.0, "2026-03": 3.1 } }
            """);

        var error = Assert.Throws<DataLoadException>(() => store.LoadMonthlySeries(path));

        Assert.Contains("brakuje miesiąca 2026-02", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsAMonthBeyondKnownThrough()
    {
        var path = WriteTemporary("cpi.json", """
            { "name": "x", "knownThrough": "2026-01",
              "percent": { "2026-01": 3.0, "2026-05": 3.1 } }
            """);

        var error = Assert.Throws<DataLoadException>(() => store.LoadMonthlySeries(path));

        Assert.Contains("za 'knownThrough'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsADateBeyondKnownThrough()
    {
        var path = WriteTemporary("nbp.json", """
            { "name": "x", "knownThrough": "2026-01-01",
              "percent": { "2025-01-01": 3.0, "2026-03-01": 3.1 } }
            """);

        var error = Assert.Throws<DataLoadException>(() => store.LoadDatedSeries(path));

        Assert.Contains("za 'knownThrough'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsAMonthKeyThatIsNotAMonth()
    {
        var path = WriteTemporary("cpi.json", """
            { "name": "x", "knownThrough": "2026-01", "percent": { "styczen": 3.0 } }
            """);

        Assert.Throws<DataLoadException>(() => store.LoadMonthlySeries(path));
    }

    [Fact]
    public void ReportsADateKeyThatIsNotADate()
    {
        var path = WriteTemporary("nbp.json", """
            { "name": "x", "knownThrough": "2026-01-01", "percent": { "wczoraj": 3.0 } }
            """);

        Assert.Throws<DataLoadException>(() => store.LoadDatedSeries(path));
    }

    private static string IssueWith(string rateAndFee) => $$"""
        {
          "code": "TEST0101",
          "kind": "Coi",
          "source": "test",
          "nominalZloty": 100.00,
          "purchasePriceZloty": 100.00,
          "periodsPerYear": 1,
          "periodCount": 4,
          "capitalizes": false,
          "roundCapitalizedBase": false,
          "principalFloor": "FirstPeriodOnly",
          "sale": { "from": "2026-08-01", "to": "2026-08-31" },
          {{rateAndFee}},
          "earlyRedemption": { "minDaysAfterPurchase": 7, "minDaysBeforeMaturity": 20 }
        }
        """;

    private string WriteTemporary(string name, string content)
    {
        Directory.CreateDirectory(temporaryDirectory);
        var path = Path.Combine(temporaryDirectory, name);
        File.WriteAllText(path, content);
        return path;
    }
}
