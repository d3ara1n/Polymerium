using System.Runtime.InteropServices;

namespace MirrorChyan.Net.Models;

public static class Oses
{
    public const string Windows = "windows";
    public const string Win = "win";
    public const string Win32 = "win32";

    public const string Linux = "linux";

    public const string Darwin = "darwin";
    public const string Mac = "mac";
    public const string MacOs = "macos";
    public const string Osx = "osx";

    public const string Android = "android";

    public static readonly string[] Windowses = [Windows, Win, Win32];
    public static readonly string[] Linuxes = [Linux];
    public static readonly string[] Darwins = [Darwin, Mac, MacOs, Osx];
    public static readonly string[] Androids = [Android];

    public static bool IsWindows(string os) => Windowses.Contains(os);
    public static bool IsLinux(string os) => Linuxes.Contains(os);
    public static bool IsDarwin(string os) => Darwins.Contains(os);
    public static bool IsAndroid(string os) => Androids.Contains(os);

    public static string FromPlatform() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Windows :
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? Linux :
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? Darwin :
        throw new NotSupportedException("Unsupported operating system.");

    public static OSPlatform ToPlatform(string os)
    {
        if (IsWindows(os))
        {
            return OSPlatform.Windows;
        }

        if (IsLinux(os))
        {
            return OSPlatform.Linux;
        }

        if (IsDarwin(os))
        {
            return OSPlatform.OSX;
        }

        throw new ArgumentOutOfRangeException(nameof(os), os, null);
    }
}
