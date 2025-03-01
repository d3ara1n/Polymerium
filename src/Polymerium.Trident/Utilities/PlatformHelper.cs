using System.Runtime.InteropServices;

namespace Polymerium.Trident.Utilities;

public static class PlatformHelper
{
    public static string GetOsName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "windows";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "osx";

        throw new NotSupportedException("Unsupported operating system.");
    }

    public static string GetOsArch() =>
        RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => throw new NotSupportedException("Unsupported process architecture.")
        };

    public static string GetOsVersion() => Environment.OSVersion.Version.ToString();
}