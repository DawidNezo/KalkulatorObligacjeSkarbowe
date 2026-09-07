namespace Bonds.Sources.Tests.Support;

/// <summary>
/// Finds the files of the repository from the directory the tests run in. A test that
/// reads a shipped source document needs the source tree and not the output
/// directory.
///
/// The walk starts at the output directory and not at the path of this source file:
/// a deterministic CI build maps every source path to "/_/", which leaves no
/// directory to walk.
/// </summary>
internal static class RepositoryPaths
{
    internal static string Root { get; } = FindRoot(AppContext.BaseDirectory);

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

    private static string FindRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bonds.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"No Bonds.sln found above '{startDirectory}'.");
    }
}
