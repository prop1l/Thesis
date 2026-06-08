using System;
using System.IO;

namespace Thesis.Tests;

public sealed class TestFolder : IDisposable
{
    public string PathValue { get; }

    public TestFolder()
    {
        PathValue = Path.Combine(
            Path.GetTempPath(),
            "Thesis.Tests",
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(PathValue);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(PathValue))
                Directory.Delete(PathValue, true);
        }
        catch
        {
        }
    }
}