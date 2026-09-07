namespace Bonds.Core;

/// <summary>Shared clean-up of messages that a person is going to read.</summary>
public static class ErrorMessages
{
    /// <summary>
    /// An <see cref="ArgumentException"/> ends its message with "(Parameter 'X')".
    /// The parameter name means nothing to the person at the terminal or to the
    /// person who edits a data file.
    /// </summary>
    public static string WithoutParameterName(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var suffix = message.LastIndexOf(" (Parameter '", StringComparison.Ordinal);
        return suffix < 0 ? message : message[..suffix];
    }
}
