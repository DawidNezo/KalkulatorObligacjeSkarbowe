namespace Bonds.Cli.Tests.Support;

/// <summary>
/// Finds the files of the repository from the directory the tests run in. A test
/// that runs the tool against the shipped data needs the source tree and not the
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

    private static string FindRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
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
