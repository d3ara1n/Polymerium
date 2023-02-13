using System;
using System.Collections.Generic;
using Polymerium.Core.Models.Forge.InstallerVersions;

namespace Polymerium.Core.Models.Forge;

public struct InstallerVersion
{
    public string Id { get; set; }
    public DateTimeOffset Time { get; set; }
    public DateTimeOffset ReleaseTime { get; set; }
    public ReleaseType Type { get; set; }
    public string MainClass { get; set; }
    public string InheritsFrom { get; set; }
    public ForgeArguments? Arguments { get; set; }
    public string MinecraftArguments { get; set; }
    public IEnumerable<ForgeLibrary> Libraries { get; set; }
}
