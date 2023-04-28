using Polymerium.Core.Stars.Facilities;
using System;
using System.Diagnostics;
using System.Linq;

namespace Polymerium.Core.Stars;

public class PlanetaryEngine : IDisposable
{
    private readonly PlanetaryEngineOptions _options;

    public bool IsFired { get; private set; }

    public Process? Core { get; private set; }

    public StreamBuffer<PlanetScrap> Output { get; } = new();

    private ulong lineReceived = 0;

    public PlanetaryEngine(PlanetaryEngineOptions options)
    {
        _options = options;
    }

    public void LaunchFireForget()
    {
        if (IsFired)
            throw new InvalidOperationException();
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
        IsFired = true;
    }

    public void LaunchManaged()
    {
        if (IsFired)
            throw new InvalidOperationException();
        Core = new Process
        {
            StartInfo = new ProcessStartInfo(_options.JavaExecutable)
            {
                WorkingDirectory = _options.WorkingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };
        foreach (var item in _options.Arguments.Where(x => !string.IsNullOrWhiteSpace(x)))
            Core.StartInfo.ArgumentList.Add(item);
        Core.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                OutputDataReceived(e.Data, PlanetScrapSeverity.Unknown);
        };
        Core.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                OutputDataReceived(e.Data, PlanetScrapSeverity.Error);
        };
        Core.Start();
        IsFired = true;
    }

    private void OutputDataReceived(string line, PlanetScrapSeverity severity)
    {
        var scrap = new PlanetScrap()
        {
            Index = lineReceived++,
            Line = line,
            Severity = severity,
            Source = null
        };
        Output.Add(scrap);
    }

    public void Dispose() => Dispose(isDisposing);

    private bool isDisposing = false;

    protected void Dispose(bool disposed)
    {
        if (!disposed)
        {
            disposed = true;
            // do clean
        }
    }
}
