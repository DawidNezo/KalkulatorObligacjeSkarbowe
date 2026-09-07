using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Bonds.Sources.Letters;

/// <summary>One field of an issue file and the paragraphs that settle it.</summary>
public readonly record struct FieldHint(string Field, ImmutableArray<LetterParagraph> Paragraphs);

/// <summary>
/// Points at the paragraph that settles each field of an issue file.
/// <para>
/// The numbering of the paragraphs moves between the families of bonds and between
/// the months of the same family: the effects of an early redemption are in
/// paragraph 28 of a ROR letter and in paragraph 26 of a COI letter. Thus the field
/// "source" cannot be copied from the file of the month before, and this class
/// exists to read the numbers out of the letter at hand.
/// </para>
/// </summary>
public static partial class IssueFieldHints
{
    /// <summary>
    /// The fields, in the order they appear in an issue file, with the sentence that
    /// settles each. The order lets a reader go through the letter once, downwards.
    /// </summary>
    public static ImmutableArray<(string Field, Regex Sentence)> Fields =>
    [
        ("sale.from, sale.to", SaleWindow()),
        ("nominalZloty", Nominal()),
        ("purchasePriceZloty", Price()),
        ("periodsPerYear, capitalizes", InterestAccrual()),
        ("rate.firstPeriodRatePercent", FirstPeriodRate()),
        ("rate.type, rate.marginPercent, periodCount", LaterPeriodRate()),
        ("capitalizes (wypłata albo kapitalizacja)", InterestPayment()),
        ("kwota do sprawdzenia wyniku", StatedAmount()),
        ("earlyRedemption.minDaysAfterPurchase, minDaysBeforeMaturity", EarlyRedemptionRight()),
        ("fee.type, fee.amountZloty", EarlyRedemptionEffect()),
    ];

    /// <summary>
    /// The hints for one letter. A field with no paragraph comes back with an empty
    /// array and not missing: a field that the reader cannot find is the one that
    /// the person has to look for by hand, thus it must stay visible.
    /// </summary>
    public static ImmutableArray<FieldHint> For(IssueLetter letter)
    {
        ArgumentNullException.ThrowIfNull(letter);

        var hints = ImmutableArray.CreateBuilder<FieldHint>(Fields.Length);
        foreach (var (field, sentence) in Fields)
        {
            hints.Add(new FieldHint(field,
                [.. letter.Paragraphs.Where(paragraph => sentence.IsMatch(paragraph.Text))]));
        }

        return hints.MoveToImmutable();
    }

    [GeneratedRegex(@"oferowane (przez agenta emisji )?do sprzedaży w dniach", RegexOptions.IgnoreCase)]
    private static partial Regex SaleWindow();

    [GeneratedRegex(@"Nominał jednej obligacji wynosi", RegexOptions.IgnoreCase)]
    private static partial Regex Nominal();

    [GeneratedRegex(@"Cena sprzedaży", RegexOptions.IgnoreCase)]
    private static partial Regex Price();

    [GeneratedRegex(@"Odsetki (od obligacji )?(są )?nalicza", RegexOptions.IgnoreCase)]
    private static partial Regex InterestAccrual();

    [GeneratedRegex(
        @"W pierwszym okresie odsetkowym stopa procentowa wynosi"
        + @"|Oprocentowanie obligacji jest stałe"
        + @"|Oprocentowanie obligacji wynosi",
        RegexOptions.IgnoreCase)]
    private static partial Regex FirstPeriodRate();

    [GeneratedRegex(
        @"Począwszy od drugiego okresu odsetkowego stopa procentowa"
        + @"|Stopa procentowa od drugiego do \p{L}+ okresu odsetkowego",
        RegexOptions.IgnoreCase)]
    private static partial Regex LaterPeriodRate();

    [GeneratedRegex(
        @"Należność z tytułu odsetek jest wypłacana|Wypłata odsetek następuje",
        RegexOptions.IgnoreCase)]
    private static partial Regex InterestPayment();

    [GeneratedRegex(
        @"wierzytelności z tytułu wykupu jednej obligacji"
        + @"|Wysokość należnych odsetek w dniu wykupu",
        RegexOptions.IgnoreCase)]
    private static partial Regex StatedAmount();

    [GeneratedRegex(@"prawo wezwania emitenta do przedterminowego wykupu", RegexOptions.IgnoreCase)]
    private static partial Regex EarlyRedemptionRight();

    [GeneratedRegex(
        @"W przypadku skorzystania przez posiadacza obligacji z uprawnienia",
        RegexOptions.IgnoreCase)]
    private static partial Regex EarlyRedemptionEffect();
}
