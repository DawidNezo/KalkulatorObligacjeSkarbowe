using Bonds.Sources.Letters;
using Bonds.Sources.Tests.Support;
using Xunit;

namespace Bonds.Sources.Tests;

/// <summary>
/// The paragraph reader rests on one property of the letters: the numbering starts
/// at one and skips nothing. Every other number in the text has to fall away, and
/// the text of a letter is full of them.
/// </summary>
public sealed class IssueLetterTests
{
    /// <summary>
    /// A body with every trap that a real letter holds: a page number alone on a
    /// line, a cross reference to another paragraph, sub-points, an amount, a date
    /// and a paragraph that wraps over several lines.
    /// </summary>
    private const string Body = """
                     LIST EMISYJNY nr 86/2026 Ministra Finansów i Gospodarki
                                    z dnia 20 sierpnia 2026 r.

        emituje czteroletnie obligacje, o czterech rocznych okresach odsetkowych,
        o nazwie skróconej COI0930, zwane dalej „obligacjami”.

        1. Do sprzedaży są oferowane obligacje o łącznej wartości nominalnej
           5.000.000.000 zł (pięć miliardów złotych).

        2. Nominał jednej obligacji wynosi 100 zł (sto złotych).

        1

        3. Obligacje są oferowane do sprzedaży w dniach od 1 do 30 września 2026 r.:
           1) w punktach sprzedaży obligacji;
           2) za pośrednictwem systemów teleinformatycznych.

        4. W pierwszym okresie odsetkowym stopa procentowa wynosi 4,75% w skali roku.

        2

        5. W przypadku skorzystania przez posiadacza obligacji z uprawnienia,
           o którym mowa w ust. 4: 1) posiadacz składa dyspozycję; 2) należność jest
           pomniejszana o kwotę narosłych odsetek, ale nie wyższą niż 2,00 zł.

        Załącznik nr 1
        Sposób obliczenia stopy procentowej

        r=i+m
        """;

    [Fact]
    public void ReadsEveryParagraphAndNothingElse()
    {
        var letter = IssueLetter.Parse(Body);

        Assert.Equal([1, 2, 3, 4, 5], letter.Paragraphs.Select(p => p.Number));
    }

    [Fact]
    public void DropsAPageNumberThatSitsAloneOnItsLine()
    {
        // "1" and "2" between the paragraphs are page numbers. Read as paragraphs
        // they would restart the numbering and cut the letter in three.
        var letter = IssueLetter.Parse(Body);

        Assert.Equal(5, letter.Paragraphs.Length);
    }

    [Fact]
    public void StartsANewParagraphAfterAPageBreak()
    {
        // "pdftotext" writes a form feed at every page boundary and writes no line
        // break with it, thus the first paragraph of a page follows the form feed
        // straight away. A reader that splits on the line break alone loses this
        // paragraph, and with it every paragraph after it, because the run of
        // numbers breaks. This is what happened on the real letter of COI0930,
        // where paragraph 22 opens page three.
        var letter = IssueLetter.Parse("1. Pierwszy ustęp.\n2. Drugi ustęp.\f3. Trzeci ustęp.\n");

        Assert.Equal([1, 2, 3], letter.Paragraphs.Select(p => p.Number));
        Assert.Equal("3. Trzeci ustęp.", letter.Paragraph(3)!.Value.Text);
    }

    [Fact]
    public void ReadsALetterWrittenWithWindowsLineBreaks()
    {
        var letter = IssueLetter.Parse("1. Pierwszy ustęp.\r\n2. Drugi ustęp.\r\n");

        Assert.Equal([1, 2], letter.Paragraphs.Select(p => p.Number));
    }

    [Fact]
    public void KeepsACrossReferenceInsideTheParagraphThatHoldsIt()
    {
        // "o którym mowa w ust. 4" is a reference and not the start of paragraph 4.
        var fifth = IssueLetter.Parse(Body).Paragraph(5);

        Assert.NotNull(fifth);
        Assert.Contains("o którym mowa w ust. 4", fifth.Value.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void PutsAParagraphThatWrapsOverLinesOnOneLine()
    {
        var first = IssueLetter.Parse(Body).Paragraph(1);

        Assert.NotNull(first);
        Assert.DoesNotContain('\n', first.Value.Text);
        Assert.Contains("5.000.000.000 zł", first.Value.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void CutsTheAnnexesOffTheBody()
    {
        var letter = IssueLetter.Parse(Body);

        // The formulas are in the annexes and the fields come from the paragraphs,
        // thus a heading in an annex must not become a paragraph.
        Assert.Contains("r=i+m", letter.Annexes, StringComparison.Ordinal);
        Assert.All(letter.Paragraphs,
            paragraph => Assert.DoesNotContain("r=i+m", paragraph.Text, StringComparison.Ordinal));
    }

    [Fact]
    public void ReadsTheNumberDateAndCodeOfTheLetter()
    {
        var headline = IssueLetter.Parse(Body).Headline;

        Assert.Contains("nr 86/2026", headline, StringComparison.Ordinal);
        Assert.Contains("20 sierpnia 2026", headline, StringComparison.Ordinal);
        Assert.Contains("COI0930", headline, StringComparison.Ordinal);
        Assert.Contains("czterech rocznych", headline, StringComparison.Ordinal);
    }

    [Fact]
    public void SaysSoWhenItCannotReadTheHeadline()
    {
        var headline = IssueLetter.Parse("Zupełnie inny dokument.").Headline;

        Assert.Contains("Nie rozpoznano nagłówka", headline, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsNoParagraphForATextWithNone() =>
        Assert.Empty(IssueLetter.Parse("Zupełnie inny dokument.").Paragraphs);

    [Fact]
    public void LeavesTheAnnexesEmptyForALetterWithout()
    {
        // The OTS letter has no annex, because a three month bond needs no formula.
        var letter = IssueLetter.Parse("1. Jedyny ustęp.");

        Assert.Empty(letter.Annexes);
    }
}

/// <summary>
/// The same reader, on the real letter of COI0930. This is the letter on which the
/// paragraph of the early redemption turned out to be 26 and not 28, as the issue
/// files of August claimed.
/// </summary>
public sealed class RealIssueLetterTests
{
    private static IssueLetter Letter { get; } =
        IssueLetter.Parse(File.ReadAllText(RepositoryPaths.Fixture("list-coi0930.txt")));

    [Fact]
    public void NumbersTheParagraphsFromOneWithNoGap() =>
        Assert.Equal(Enumerable.Range(1, Letter.Paragraphs.Length),
            Letter.Paragraphs.Select(p => p.Number));

    [Fact]
    public void ReadsTheThirtyFourParagraphsOfTheLetter() =>
        Assert.Equal(34, Letter.Paragraphs.Length);

    [Fact]
    public void ReadsTheHeadlineOfTheRealLetter()
    {
        Assert.Contains("nr 86/2026", Letter.Headline, StringComparison.Ordinal);
        Assert.Contains("COI0930", Letter.Headline, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("sale.from, sale.to", 4)]
    [InlineData("nominalZloty", 2)]
    [InlineData("purchasePriceZloty", 7)]
    [InlineData("periodsPerYear, capitalizes", 14)]
    [InlineData("rate.firstPeriodRatePercent", 15)]
    [InlineData("rate.type, rate.marginPercent, periodCount", 16)]
    [InlineData("capitalizes (wypłata albo kapitalizacja)", 19)]
    [InlineData("earlyRedemption.minDaysAfterPurchase, minDaysBeforeMaturity", 23)]
    [InlineData("fee.type, fee.amountZloty", 26)]
    public void PointsAtTheParagraphThatSettlesEachField(string field, int paragraph)
    {
        var hint = IssueFieldHints.For(Letter).Single(h => h.Field == field);

        Assert.Contains(paragraph, hint.Paragraphs.Select(p => p.Number));
    }

    [Fact]
    public void FindsTheRateAndTheMarginInTheParagraphItPointsAt()
    {
        var hint = IssueFieldHints.For(Letter)
            .Single(h => h.Field == "rate.type, rate.marginPercent, periodCount");

        var text = hint.Paragraphs.Single(p => p.Number == 16).Text;
        Assert.Contains("stopy wzrostu cen towarów i usług konsumpcyjnych", text, StringComparison.Ordinal);
        Assert.Contains("marżę w wysokości 1,50%", text, StringComparison.Ordinal);
        Assert.Contains("od drugiego do czwartego okresu odsetkowego", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FindsTheFeeAmountInTheParagraphItPointsAt()
    {
        var text = Letter.Paragraph(26)!.Value.Text;

        Assert.Contains("nie wyższą niż 2,00 zł", text, StringComparison.Ordinal);
        Assert.Contains("począwszy od drugiego okresu odsetkowego", text, StringComparison.Ordinal);
    }

    [Fact]
    public void KeepsTheFieldVisibleWhenTheLetterStatesNoAmountToCheck()
    {
        // The COI letter states no ready result, unlike the letters of OTS and TOS.
        // An empty hint has to stay in the list, so that the person sees that this
        // is the field to look for by hand.
        var hint = IssueFieldHints.For(Letter).Single(h => h.Field == "kwota do sprawdzenia wyniku");

        Assert.Empty(hint.Paragraphs);
    }

    [Fact]
    public void ReadsTheAnnexWithTheRateFormula() =>
        Assert.Contains("r=i+m", Letter.Annexes, StringComparison.Ordinal);
}
