namespace Bonds.Sources;

/// <summary>
/// A source document is not what the reader expects. The message names the file
/// and what is wrong with it, because the person who reads it has to decide
/// whether to download the file again or to fix the reader.
/// </summary>
public sealed class SourceException : Exception
{
    public SourceException(string path, string problem)
        : base($"{path}: {problem}") =>
        Path = path;

    public SourceException(string path, string problem, Exception inner)
        : base($"{path}: {problem}", inner) =>
        Path = path;

    public string Path { get; }
}
