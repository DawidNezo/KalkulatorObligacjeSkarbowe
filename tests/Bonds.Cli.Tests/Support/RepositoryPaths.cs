using System.Runtime.CompilerServices;

namespace Bonds.Cli.Tests.Support;

/// <summary>
/// Finds the files of the repository from the path of this source file. A test
/// that runs the tool against the shipped data needs the source tree and not the
/// output directory.
/// </summary>
internal static class RepositoryPaths
{
    internal static string Root { get; } = FindRoot(ThisFile());

    internal static string Data => Path.Combine(Root, "data");

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

            directory = directory.Parent!;
        }

        throw new InvalidOperationException("The repository root with Bonds.sln was not found.");
    }
}
