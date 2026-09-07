using System.Globalization;

namespace Bonds.Core.CommandLine;

/// <summary>
/// The steps that the two hand-written parsers of this repository share. Each
/// tool keeps its own option names and checks; only the mechanics live here.
/// </summary>
public static class CommandLineArguments
{
    /// <summary>
    /// The value of the option at <paramref name="index"/>. A token that starts
    /// with "--" is the next option, not a value. Without this check
    /// "--out --help" would write a file named "--help".
    /// </summary>
    public static string Value(IReadOnlyList<string> arguments, ref int index)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var name = arguments[index];
        if (index + 1 >= arguments.Count
            || arguments[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new CommandLineException($"Opcja '{name}' wymaga wartości.");
        }

        index++;
        return arguments[index];
    }

    public static DateOnly ParseDate(string text, string option) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date)
            ? date
            : throw new CommandLineException($"Opcja '{option}' wymaga daty RRRR-MM-DD; '{text}' nią nie jest.");
}
