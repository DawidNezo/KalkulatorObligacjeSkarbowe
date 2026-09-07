using Bonds.Core.Data;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class DataRootLocatorTests : IDisposable
{
    private readonly string temporaryDirectory =
        Path.Combine(Path.GetTempPath(), "bonds-dataroot-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public void WalksUpToTheDirectoryThatHoldsData()
    {
        var data = Path.Combine(temporaryDirectory, "data");
        var deep = Path.Combine(temporaryDirectory, "src", "Tool", "bin", "Release", "net10.0");
        Directory.CreateDirectory(data);
        Directory.CreateDirectory(deep);

        Assert.Equal(data, DataRootLocator.Find(deep));
    }

    [Fact]
    public void FallsBackToTheWorkingDirectoryWhenNothingIsFound()
    {
        var deep = Path.Combine(temporaryDirectory, "alone");
        Directory.CreateDirectory(deep);

        Assert.Equal("data", DataRootLocator.Find(deep));
    }
}
