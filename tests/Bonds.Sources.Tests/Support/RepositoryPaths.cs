using System.Runtime.CompilerServices;

namespace Bonds.Sources.Tests.Support;

/// <summary>
/// Finds the files of the repository from the path of this source file. A test that
/// reads a shipped source document needs the source tree and not the output
/// directory.
/// </summary>
internal static class RepositoryPaths
{
    internal static string Root { get; } = FindRoot(ThisFile());

    internal static string Data => Path.Combine(Root, "data");

    /// <summary>Third-party source documents live in zrodla/, outside data/.</summary>
    internal static string NbpSources => Path.Combine(Root, "zrodla", "nbp");

    internal static string NbpArchive =>
        Path.Combine(NbpSources, CommandLineOptions.NbpArchiveName);

    internal static string CpiWorkbook =>
        Path.Combine(NbpSources, CommandLineOptions.CpiWorkbookName);

    internal static string Letters => Path.Combine(Root, "zrodla", "listy_emisyjne");

    internal static string Fixture(string name) =>
        Path.Combine(Root, "tests", "Bonds.Sources.Tests", "Fixtures", name);

    private static string ThisFile([CallerFilePath] string path = "") => path;

    private static string FindRoot(string startFile)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(startFile)!);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bonds.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"No Bonds.sln found above '{startFile}'.");
    }
}
