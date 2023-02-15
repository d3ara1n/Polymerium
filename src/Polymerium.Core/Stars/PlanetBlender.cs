using System.Diagnostics;

namespace Polymerium.Core.Stars;

public class PlanetBlender
{
    private readonly PlanetOptions _options;

    public PlanetBlender(PlanetOptions options)
    {
        _options = options;
    }

    public void Start()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo(_options.JavaExecutable)
            {
                WorkingDirectory = _options.WorkingDirectory
            }
        };
        foreach (var item in _options.Arguments)
            process.StartInfo.ArgumentList.Add(item);
        process.Start();
    }
}