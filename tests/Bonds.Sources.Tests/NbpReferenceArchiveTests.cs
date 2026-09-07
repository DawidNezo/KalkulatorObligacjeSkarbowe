using Bonds.Sources.Market;
using Bonds.Sources.Tests.Support;
using Xunit;

namespace Bonds.Sources.Tests;

public sealed class NbpReferenceArchiveTests : IDisposable
{
    private readonly string temporaryDirectory =
        Path.Combine(Path.GetTempPath(), "bonds-nbp-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public void ReadsTheReferenceRateAndIgnoresTheOtherRates()
    {
        // The archive holds several rates per decision. Only "ref" is the one that
        // the letters of ROR and DOR name.
        var rates = NbpReferenceArchive.Read(Archive("""
            <?xml version="1.0" encoding="utf-8"?>
            <stopy_procentowe_archiwum>
              <pozycje obowiazuje_od="2025-12-04">
                <pozycja id="ref" oprocentowanie="4,00" />
                <pozycja id="lom" oprocentowanie="4,50" />
                <pozycja id="dep" oprocentowanie="3,50" />
              </pozycje>
              <pozycje obowiazuje_od="2026-03-05">
                <pozycja id="ref" oprocentowanie="3,75" />
                <pozycja id="lom" oprocentowanie="4,25" />
              </pozycje>
            </stopy_procentowe_archiwum>
            """));

        Assert.Equal([new DateOnly(2025, 12, 4), new DateOnly(2026, 3, 5)], rates.Keys);
        Assert.Equal(4.00m, rates[new DateOnly(2025, 12, 4)]);
        Assert.Equal(3.75m, rates[new DateOnly(2026, 3, 5)]);
    }

    [Fact]
    public void ReadsAPercentWrittenWithAComma()
    {
        // The archive writes 3,75 and not 3.75. A reader that follows the machine
        // locale would give 375 on a Polish machine.
        var rates = NbpReferenceArchive.Read(Archive("""
            <stopy_procentowe_archiwum>
              <pozycje obowiazuje_od="2026-03-05"><pozycja id="ref" oprocentowanie="3,75" /></pozycje>
            </stopy_procentowe_archiwum>
            """));

        Assert.Equal(3.75m, rates.Values.Single());
    }

    [Fact]
    public void SortsTheDecisionsByTheDayTheyTakeEffect()
    {
        var rates = NbpReferenceArchive.Read(Archive("""
            <stopy_procentowe_archiwum>
              <pozycje obowiazuje_od="2026-03-05"><pozycja id="ref" oprocentowanie="3,75" /></pozycje>
              <pozycje obowiazuje_od="2020-01-01"><pozycja id="ref" oprocentowanie="1,50" /></pozycje>
            </stopy_procentowe_archiwum>
            """));

        Assert.Equal([new DateOnly(2020, 1, 1), new DateOnly(2026, 3, 5)], rates.Keys);
    }

    [Fact]
    public void RefusesTheSameDayTwice()
    {
        var error = Assert.Throws<SourceException>(() => NbpReferenceArchive.Read(Archive("""
            <stopy_procentowe_archiwum>
              <pozycje obowiazuje_od="2026-03-05"><pozycja id="ref" oprocentowanie="3,75" /></pozycje>
              <pozycje obowiazuje_od="2026-03-05"><pozycja id="ref" oprocentowanie="3,50" /></pozycje>
            </stopy_procentowe_archiwum>
            """)));

        Assert.Contains("2026-03-05", error.Message, StringComparison.Ordinal);
        Assert.Contains("więcej niż raz", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RefusesAnArchiveWithNoReferenceRate()
    {
        var error = Assert.Throws<SourceException>(() => NbpReferenceArchive.Read(Archive("""
            <stopy_procentowe_archiwum>
              <pozycje obowiazuje_od="2026-03-05"><pozycja id="lom" oprocentowanie="4,25" /></pozycje>
            </stopy_procentowe_archiwum>
            """)));

        Assert.Contains(NbpReferenceArchive.ReferenceRateId, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RefusesABlockWithNoDate()
    {
        var error = Assert.Throws<SourceException>(() => NbpReferenceArchive.Read(Archive("""
            <stopy_procentowe_archiwum>
              <pozycje><pozycja id="ref" oprocentowanie="3,75" /></pozycje>
            </stopy_procentowe_archiwum>
            """)));

        Assert.Contains("obowiazuje_od", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RefusesADateThatIsNotADate()
    {
        var error = Assert.Throws<SourceException>(() => NbpReferenceArchive.Read(Archive("""
            <stopy_procentowe_archiwum>
              <pozycje obowiazuje_od="05.03.2026"><pozycja id="ref" oprocentowanie="3,75" /></pozycje>
            </stopy_procentowe_archiwum>
            """)));

        Assert.Contains("yyyy-MM-dd", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RefusesARateThatIsNotANumber()
    {
        var error = Assert.Throws<SourceException>(() => NbpReferenceArchive.Read(Archive("""
            <stopy_procentowe_archiwum>
              <pozycje obowiazuje_od="2026-03-05"><pozycja id="ref" oprocentowanie="brak" /></pozycje>
            </stopy_procentowe_archiwum>
            """)));

        Assert.Contains("nie jest liczbą", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RefusesTextThatIsNotXml()
    {
        var error = Assert.Throws<SourceException>(() => NbpReferenceArchive.Read(Archive("to nie XML")));

        Assert.Contains("nie można odczytać XML", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsAMissingFile()
    {
        var error = Assert.Throws<SourceException>(
            () => NbpReferenceArchive.Read(Path.Combine(temporaryDirectory, "nie-ma.xml")));

        Assert.Contains("nie istnieje", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadsTheArchiveThatTheRepositoryShips()
    {
        var rates = NbpReferenceArchive.Read(RepositoryPaths.NbpArchive);

        // The series has to reach far enough back for any purchase date, and the
        // last decision has to be a plausible rate.
        Assert.True(rates.Keys.Min() < new DateOnly(2000, 1, 1));
        Assert.InRange(rates[rates.Keys.Max()], 0m, 30m);
    }

    private string Archive(string content)
    {
        Directory.CreateDirectory(temporaryDirectory);
        var path = Path.Combine(temporaryDirectory, "archiwum.xml");
        File.WriteAllText(path, content);
        return path;
    }
}
