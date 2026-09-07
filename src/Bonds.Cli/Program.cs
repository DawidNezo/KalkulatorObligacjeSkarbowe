using System.Text;
using Bonds.Cli;
using Bonds.Core.Data;

// The legacy Windows console mangles Polish diacritics without this; macOS and
// Linux terminals are UTF-8 already.
if (OperatingSystem.IsWindows())
{
    try
    {
        Console.OutputEncoding = Encoding.UTF8;
    }
    catch (IOException)
    {
        // A detached console refuses the change; the output still works.
    }
}

return CliApplication.Run(
    args,
    Console.Out,
    Console.Error,
    DateOnly.FromDateTime(DateTime.Today),
    DataRootLocator.Find(AppContext.BaseDirectory));
