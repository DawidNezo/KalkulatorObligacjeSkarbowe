namespace Bonds.Core.Tests.Support;

/// <summary>
/// Finds the files of the repository from the directory the tests run in. A test that
/// reads the shipped data or writes a snapshot needs the source tree and not the
/// output directory.
///
/// The walk starts at the output directory and not at the path of this source file:
/// a deterministic CI build maps every source path to "/_/", which leaves no
/// directory to walk.
/// </summary>
internal static class RepositoryPaths
{
    internal static string Root { get; } = FindRoot(AppContext.BaseDirectory);

    internal static string Data => Path.Combine(Root, "data");

    internal static string Issues => Path.Combine(Data, "issues");

    /// <summary>The issue files of one sale month, such as "2026-09".</summary>
    internal static string IssuesFor(string month) => Path.Combine(Issues, month);

    /// <summary>Every sale month that the repository ships, in order.</summary>
    internal static IReadOnlyList<string> IssueMonths =>
        [.. Directory.GetDirectories(Issues).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal)];

    internal static string Snapshots => Path.Combine(Root, "tests", "Bonds.Core.Tests", "Snapshots");

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
