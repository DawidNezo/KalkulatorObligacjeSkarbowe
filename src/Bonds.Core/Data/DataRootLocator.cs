namespace Bonds.Core.Data;

/// <summary>
/// Finds the "data" directory of the repository by walking up from a start
/// directory. The default paths then do not depend on the working directory, thus
/// the tool runs correctly from any place.
/// </summary>
public static class DataRootLocator
{
    public static string Find(string startDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startDirectory);

        for (var directory = new DirectoryInfo(startDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        // No repository above the start directory: fall back to the working
        // directory, and the loader will name the missing path in its error.
        return "data";
    }
}
