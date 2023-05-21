using System;
using System.Diagnostics;
using System.Linq;
using Polymerium.Core.Stars.Facilities;

namespace Polymerium.Core.Stars;

public class PlanetaryEngine : IDisposable
{
    private readonly PlanetaryEngineOptions _options;

    private readonly bool isDisposing = false;

    private ulong lineReceived;

    public PlanetaryEngine(PlanetaryEngineOptions options)
    {
        _options = options;
    }

    public bool IsFired { get; private set; }

    public Process? Core { get; private set; }

    public StreamBuffer<PlanetScrap> Output { get; } = new();

    public void Dispose()
    {
        Dispose(isDisposing);
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
        var scrap = new PlanetScrap
        {
            Index = lineReceived++,
            Line = line,
            Severity = severity,
            Source = null
        };
        Output.Add(scrap);
    }

    protected void Dispose(bool disposed)
    {
        if (!disposed) disposed = true;
        // do clean
    }
}