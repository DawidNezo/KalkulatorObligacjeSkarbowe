namespace Bonds.Core.CommandLine;

/// <summary>The command line cannot be understood.</summary>
public sealed class CommandLineException : Exception
{
    public CommandLineException(string message) : base(message)
    {
    }
}
