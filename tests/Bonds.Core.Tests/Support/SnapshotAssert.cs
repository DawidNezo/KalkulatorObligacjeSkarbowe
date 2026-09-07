using Xunit;

namespace Bonds.Core.Tests.Support;

/// <summary>
/// Compares text against a file that the repository holds. A change in the output
/// of a writer must be a deliberate change, thus a difference fails the test. When
/// the file is absent, the text is written and the test fails once with a note, so
/// that a person can read the new file before they accept it.
/// </summary>
internal static class SnapshotAssert
{
    internal static void Matches(string name, string actual)
    {
        Directory.CreateDirectory(RepositoryPaths.Snapshots);
        var path = Path.Combine(RepositoryPaths.Snapshots, name);

        if (!File.Exists(path))
        {
            File.WriteAllText(path, actual);
            Assert.Fail($"Snapshot '{name}' did not exist. It is written now; read it and run the test again.");
        }

        var expected = Normalise(File.ReadAllText(path));
        if (Normalise(actual) == expected)
        {
            return;
        }

        var receivedPath = path + ".received";
        File.WriteAllText(receivedPath, actual);
        Assert.Fail($"Snapshot '{name}' differs. The new text is in '{receivedPath}'.");
    }

    private static string Normalise(string text) => text.ReplaceLineEndings("\n").TrimEnd();
}
