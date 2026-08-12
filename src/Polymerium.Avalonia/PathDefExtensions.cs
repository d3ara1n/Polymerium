using System.IO;
using TridentCore.Abstractions;

namespace Polymerium.Avalonia;

public static class PathDefExtensions
{
    extension (PathDef self)
    {
        public string FileOfConfiguration() => Path.Combine(PathDef.Default.PrivateConfigDirectory(), "settings.json");
        public string FileOfTelemetrySwitch() => Path.Combine(PathDef.Default.PrivateConfigDirectory(), "_no_telemetry_");
        public string FileOfFirstRun() => Path.Combine(PathDef.Default.PrivateConfigDirectory(), "first_run");
        public string FileOfSymlink() => Path.Combine(PathDef.Default.PrivateConfigDirectory(), "symlink");
    }
}
