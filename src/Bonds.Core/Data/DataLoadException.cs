namespace Bonds.Core.Data;

/// <summary>A data file is missing, malformed or incomplete.</summary>
public sealed class DataLoadException : Exception
{
    public DataLoadException(string path, string problem)
        : base($"{path}: {problem}") => Path = path;

    public DataLoadException(string path, string problem, Exception inner)
        : base($"{path}: {problem}", inner) => Path = path;

    public string Path { get; }
}
