using System;
using System.Text.RegularExpressions;

namespace Polymerium.Core.Models.Mojang.Indexes;

public struct RuleOs
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string Arch { get; set; }

    public bool Match()
    {
        var name = OperatingSystem.IsWindows()
            ? "windows"
            : OperatingSystem.IsMacOS()
                ? "osx"
                : OperatingSystem.IsLinux()
                    ? "linux"
                    : "unknown";
        var arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        var version = Environment.OSVersion.Version.ToString();

        if (!string.IsNullOrEmpty(Name) && !Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(Version) && !Regex.IsMatch(version, Version))
            return false;
        if (!string.IsNullOrEmpty(Arch) && !Arch.Equals(arch, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }
}
