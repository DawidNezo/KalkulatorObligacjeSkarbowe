using System.Runtime.CompilerServices;

namespace Bonds.Core.Tests.Support;

/// <summary>
/// Finds the files of the repository from the path of this source file. A test that
/// reads the shipped data or writes a snapshot needs the source tree and not the
/// output directory.
/// </summary>
internal static class RepositoryPaths
{
    internal static string Root { get; } = FindRoot(ThisFile());

    internal static string Data => Path.Combine(Root, "data");

    internal static string Issues => Path.Combine(Data, "issues");

    /// <summary>The issue files of one sale month, such as "2026-09".</summary>
    internal static string IssuesFor(string month) => Path.Combine(Issues, month);

    /// <summary>Every sale month that the repository ships, in order.</summary>
    internal static IReadOnlyList<string> IssueMonths =>
        [.. Directory.GetDirectories(Issues).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal)];

    internal static string Snapshots => Path.Combine(Root, "tests", "Bonds.Core.Tests", "Snapshots");

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
