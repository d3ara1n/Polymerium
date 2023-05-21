namespace Polymerium.Core.Models.Forge.InstallerProfiles;

public struct ForgeProfileInstall
{
    public string MirrorList { get; set; }
    public string Target { get; set; }
    public string FilePath { get; set; }
    public string Logo { get; set; }
    public string Welcome { get; set; }
    public string Version { get; set; }
    public string Path { get; set; }
    public string ProfileName { get; set; }
    public string Minecraft { get; set; }
    public bool StripMeata { get; set; }
}