using System.Collections.Generic;
using Polymerium.Core.Models.Forge.InstallerProfiles;

namespace Polymerium.Core.Models.Forge;

public struct InstallerProfile
{
    public int? Spec { get; set; }

    // for old version install compatibility
    public ForgeProfileInstall? Install { get; set; }

    public ForgeProfileInstallVersionInfo? VersionInfo { get; set; }

    // unknown objects
    public object? Optionals { get; set; }

    // end for old version install compatibility
    public string Profile { get; set; }
    public string Version { get; set; }
    public string Path { get; set; }
    public string Minecraft { get; set; }
    public string ServerJarPath { get; set; }
    public ForgeProfileData Data { get; set; }
    public IEnumerable<ForgeLibrary> Libraries { get; set; }
    public string Icon { get; set; }
    public string Json { get; set; }
    public string Logo { get; set; }
    public string MirrorList { get; set; }
    public string Welcome { get; set; }
}