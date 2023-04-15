using System.Diagnostics;
using System.Linq;

namespace Polymerium.Core.Stars;

public class PlanetaryEngine
{
    private readonly PlanetaryEngineOptions _options;

    public PlanetaryEngine(PlanetaryEngineOptions options)
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
        foreach (var item in _options.Arguments.Where(x => !string.IsNullOrWhiteSpace(x)))
            process.StartInfo.ArgumentList.Add(item);
        process.Start();
    }
}