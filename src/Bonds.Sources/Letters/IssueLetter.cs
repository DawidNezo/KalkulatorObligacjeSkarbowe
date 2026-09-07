using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Bonds.Sources.Letters;

/// <summary>One numbered paragraph of an issue letter, on one line.</summary>
public readonly record struct LetterParagraph(int Number, string Text);

/// <summary>
/// The text of one issue letter, cut into numbered paragraphs.
/// <para>
/// The whole class works on text and never on a file, thus a test needs no PDF and
/// no external program.
/// </para>
/// </summary>
public sealed partial class IssueLetter
{
    /// <summary>The heading that starts the annexes with the formulas.</summary>
    public const string AnnexMarker = "Załącznik nr 1";

    private IssueLetter(string headline, ImmutableArray<LetterParagraph> paragraphs, string annexes)
    {
        Headline = headline;
        Paragraphs = paragraphs;
        Annexes = annexes;
    }

    /// <summary>The number of the letter, its date and the code of the issue.</summary>
    public string Headline { get; }

    public ImmutableArray<LetterParagraph> Paragraphs { get; }

    /// <summary>The annexes, as text. Empty when the letter has none, as OTS.</summary>
    public string Annexes { get; }

    public static IssueLetter Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var marker = text.IndexOf(AnnexMarker, StringComparison.Ordinal);
        var body = marker < 0 ? text : text[..marker];
        var annexes = marker < 0 ? string.Empty : text[marker..];

        return new IssueLetter(ReadHeadline(text), ReadParagraphs(body), annexes);
    }

    public LetterParagraph? Paragraph(int number)
    {
        foreach (var paragraph in Paragraphs)
        {
            if (paragraph.Number == number)
            {
                return paragraph;
            }
        }

        return null;
    }

    /// <summary>
    /// Cuts the body into numbered paragraphs.
    /// <para>
    /// A letter numbers its paragraphs from one and skips no number. Thus a run
    /// that grows by one is the list of paragraphs, and every other number in the
    /// text falls away: a page number on a line of its own, a cross reference such
    /// as "ust. 25", a sub-point "1)" and a date.
    /// </para>
    /// </summary>
    public static ImmutableArray<LetterParagraph> ReadParagraphs(string body)
    {
        ArgumentNullException.ThrowIfNull(body);

        var lines = SplitLines(body).Where(line => !PageNumber().IsMatch(line));
        var text = string.Join('\n', lines);

        var starts = new List<(int Offset, int Number)>();
        var expected = 1;
        foreach (var match in ParagraphStart().Matches(text).Cast<Match>())
        {
            if (int.Parse(match.Groups[1].ValueSpan, provider: null) == expected)
            {
                starts.Add((match.Index, expected));
                expected++;
            }
        }

        var paragraphs = ImmutableArray.CreateBuilder<LetterParagraph>(starts.Count);
        for (var index = 0; index < starts.Count; index++)
        {
            var end = index + 1 < starts.Count ? starts[index + 1].Offset : text.Length;
            paragraphs.Add(new LetterParagraph(
                starts[index].Number, Flatten(text[starts[index].Offset..end])));
        }

        return paragraphs.MoveToImmutable();
    }

    /// <summary>
    /// Cuts text into lines.
    /// <para>
    /// The form feed matters. "pdftotext" writes one at every page boundary and
    /// writes no line break with it, thus the first paragraph of a page follows the
    /// form feed straight away: "\f22. W przypadku...". A reader that splits on the
    /// line break alone leaves that paragraph inside the line before it, and then
    /// the run of numbers breaks and every paragraph after it falls away.
    /// </para>
    /// </summary>
    private static string[] SplitLines(string text) =>
        text.Split(['\n', '\r', '\f']);

    /// <summary>
    /// Puts a paragraph on one line. A letter wraps its lines wherever the page
    /// ends, and those breaks carry no meaning.
    /// </summary>
    private static string Flatten(string text) => Whitespace().Replace(text, " ").Trim();

    private static string ReadHeadline(string text)
    {
        var flat = Flatten(text);
        var parts = new List<string>();

        var number = LetterNumber().Match(flat);
        if (number.Success)
        {
            parts.Add($"List: nr {number.Groups[1].Value} z dnia {number.Groups[2].Value} r.");
        }

        var code = IssueCode().Match(flat);
        if (code.Success)
        {
            parts.Add($"Emisja: {code.Groups[1].Value}");
        }

        var periods = PeriodCount().Match(flat);
        if (periods.Success)
        {
            parts.Add($"Okresy odsetkowe: {periods.Groups[1].Value} {periods.Groups[2].Value}");
        }

        return parts.Count == 0
            ? "Nie rozpoznano nagłówka; przeczytaj pierwszą stronę listu."
            : string.Join("\n", parts);
    }

    [GeneratedRegex(@"^\s*\d+\s*$")]
    private static partial Regex PageNumber();

    [GeneratedRegex(@"(?m)^[ \t]{0,4}(\d{1,2})\.[ \t]")]
    private static partial Regex ParagraphStart();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"LIST EMISYJNY nr (\d+/\d{4})[^.]{0,80}?z dnia (\d{1,2} \p{L}+ \d{4}) r\.")]
    private static partial Regex LetterNumber();

    [GeneratedRegex(@"nazwie skróconej ([A-Z]{3}\d{4})")]
    private static partial Regex IssueCode();

    [GeneratedRegex(@"o (\p{L}+) (rocznych|miesięcznych|trzymiesięcznych) okresach odsetkowych")]
    private static partial Regex PeriodCount();
}
