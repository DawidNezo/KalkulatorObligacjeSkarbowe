using System.Collections.Immutable;
using System.Globalization;
using Bonds.Core;
using Bonds.Core.Data;
using Bonds.Core.Market;
using Bonds.Sources.Market;
using Bonds.Sources.Tests.Support;
using Xunit;

namespace Bonds.Sources.Tests;

/// <summary>
/// These are the tests that matter most in this project. The two series that this
/// class renders feed every rate of every report, and two of the rules here are
/// invisible in the result: the shift from the month a CPI figure is about to the
/// month it comes out, and the base of the index. A mistake in either would move
/// the rate of every indexed bond by a year, or by a hundred points, without any
/// error anywhere.
/// <para>
/// Each test therefore reads the rendered text back with the loader of the
/// calculator, which is the only reader that counts.
/// </para>
/// </summary>
public sealed class MarketSeriesWriterTests : IDisposable
{
    private readonly JsonDataStore store = new();
    private readonly string temporaryDirectory =
        Path.Combine(Path.GetTempPath(), "bonds-series-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public void MovesEveryCpiKeyToTheMonthOfItsPublication()
    {
        // Statistics Poland published the figure for July 2026 in August 2026, and
        // the letters name "the index published in the month before the first month
        // of the interest period". Thus the key of the July figure is 2026-08.
        var series = LoadCpi(Cpi((2026, 6, 2.50m), (2026, 7, 3.00m)));

        Assert.Equal(3.00m / 100m, series.ValueIn(YearMonth.Parse("2026-08")));
        Assert.Equal(2.50m / 100m, series.ValueIn(YearMonth.Parse("2026-07")));
    }

    [Fact]
    public void EndsTheCpiSeriesAtTheLastMonthOfPublication()
    {
        var series = LoadCpi(Cpi((2026, 6, 2.50m), (2026, 7, 3.00m)));

        // "knownThrough" is 2026-08 and not 2026-07: the August figure exists,
        // because it came out in August. A month after it is a projection.
        Assert.Equal(YearMonth.Parse("2026-08"), series.KnownThrough);
        Assert.Null(series.ValueIn(YearMonth.Parse("2026-09")));
    }

    [Fact]
    public void SaysInTheFileWhichMonthTheLastKeyIsAbout()
    {
        // The shift is the one thing a person reading the file has to understand,
        // thus the comment has to name both months and stay true after a refresh.
        var text = MarketSeriesWriter.RenderCpi(Readings((2026, 7, 3.00m)), "bazowa.xlsx");

        Assert.Contains("Klucz 2026-08 to więc", text, StringComparison.Ordinal);
        Assert.Contains("odczyt za 2026-07", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("pl-PL")]
    [InlineData("de-DE")]
    [InlineData("en-US")]
    [InlineData("ar-SA")]
    public void WritesTheSameFileOnEveryMachine(string culture)
    {
        // A locale with a decimal comma would write 3,00 and break the JSON, and a
        // calendar that is not Gregorian would write another year. The whole output
        // is therefore stated as invariant.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            var cpi = MarketSeriesWriter.RenderCpi(Readings((2026, 7, 3.00m)), "bazowa.xlsx");
            var nbp = MarketSeriesWriter.RenderNbpReference(
                Rates((2026, 3, 5, 3.75m)), new DateOnly(2026, 8, 28), "archiwum.xml");

            Assert.Contains("\"2026-08\": 3.00", cpi, StringComparison.Ordinal);
            Assert.Contains("\"2026-03-05\": 3.75", nbp, StringComparison.Ordinal);
            Assert.Contains("\"knownThrough\": \"2026-08-28\"", nbp, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void RendersAPercentWithTwoDecimals()
    {
        var text = MarketSeriesWriter.RenderCpi(Readings((2026, 7, 3m), (2026, 8, -0.5m)), "bazowa.xlsx");

        Assert.Contains("\"2026-08\": 3.00", text, StringComparison.Ordinal);
        Assert.Contains("\"2026-09\": -0.50", text, StringComparison.Ordinal);
    }

    [Fact]
    public void KeepsDeflationAsANegativeNumber()
    {
        // A negative index is a real reading. The floor at zero belongs to the rate
        // rule of the letter, not to the series, thus the file must carry the sign.
        var series = LoadCpi(Cpi((2026, 7, -0.50m)));

        Assert.Equal(-0.50m / 100m, series.ValueIn(YearMonth.Parse("2026-08")));
    }

    [Fact]
    public void WritesAnNbpSeriesThatTheCalculatorReads()
    {
        var series = store.LoadDatedSeries(File("nbp.json", MarketSeriesWriter.RenderNbpReference(
            Rates((2025, 12, 4, 4.00m), (2026, 3, 5, 3.75m)),
            new DateOnly(2026, 8, 28),
            "archiwum.xml")));

        // A dated series is a step function: a rate holds until the next decision.
        Assert.Equal(4.00m / 100m, series.ValueOn(new DateOnly(2026, 1, 15)));
        Assert.Equal(3.75m / 100m, series.ValueOn(new DateOnly(2026, 8, 28)));
        Assert.Null(series.ValueOn(new DateOnly(2026, 8, 29)));
    }

    [Fact]
    public void WritesEveryMonthOfTheRealWorkbookWithoutAGap()
    {
        // The loader refuses a monthly series with a hole. This is the end to end
        // check: the shipped workbook, rendered, and read back by the calculator.
        var readings = CpiWorkbook.Read(RepositoryPaths.CpiWorkbook);

        var series = LoadCpi(MarketSeriesWriter.RenderCpi(readings, "bazowa.xlsx"));

        Assert.Equal(
            readings.Keys.Max().AddMonths(MarketSeriesWriter.CpiPublicationLagMonths),
            series.KnownThrough);
        Assert.NotNull(series.ValueIn(series.KnownThrough));
    }

    [Fact]
    public void WritesEveryDecisionOfTheRealArchive()
    {
        var rates = NbpReferenceArchive.Read(RepositoryPaths.NbpArchive);
        var knownThrough = rates.Keys.Max().AddDays(1);

        var series = store.LoadDatedSeries(File("nbp-real.json",
            MarketSeriesWriter.RenderNbpReference(rates, knownThrough, "archiwum.xml")));

        Assert.Equal(rates.Keys.Min(), series.EarliestDate);
        Assert.Equal(rates[rates.Keys.Max()] / 100m, series.ValueOn(knownThrough));
    }

    [Fact]
    public void RefusesToRenderAnEmptyCpiSeries() =>
        Assert.Throws<ArgumentException>(() => MarketSeriesWriter.RenderCpi(
            ImmutableSortedDictionary<YearMonth, decimal>.Empty, "bazowa.xlsx"));

    private static ImmutableSortedDictionary<YearMonth, decimal> Readings(
        params (int Year, int Month, decimal Percent)[] readings) =>
        readings.ToImmutableSortedDictionary(
            reading => new YearMonth(reading.Year, reading.Month),
            reading => reading.Percent);

    private static ImmutableSortedDictionary<DateOnly, decimal> Rates(
        params (int Year, int Month, int Day, decimal Percent)[] rates) =>
        rates.ToImmutableSortedDictionary(
            rate => new DateOnly(rate.Year, rate.Month, rate.Day),
            rate => rate.Percent);

    private static string Cpi(params (int Year, int Month, decimal Percent)[] readings) =>
        MarketSeriesWriter.RenderCpi(Readings(readings), "bazowa.xlsx");

    private MonthlyRateSeries LoadCpi(string text) =>
        store.LoadMonthlySeries(File("cpi.json", text));

    private string File(string name, string text)
    {
        Directory.CreateDirectory(temporaryDirectory);
        var path = Path.Combine(temporaryDirectory, name);
        System.IO.File.WriteAllText(path, text);
        return path;
    }
}
