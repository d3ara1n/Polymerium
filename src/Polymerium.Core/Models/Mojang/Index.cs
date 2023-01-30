using System;
using System.Collections.Generic;
using Polymerium.Core.Models.Mojang.Indexes;

namespace Polymerium.Core.Models.Mojang;

public struct Index
{
    public Arguments Arguments { get; set; }
    public AssetIndex AssetIndex { get; set; }
    public string Assets { get; set; }
    public uint ComplianceLevel { get; set; }
    public IndexDownloads Downloads { get; set; }
    public string Id { get; set; }
    public JavaVersion JavaVersion { get; set; }
    public IEnumerable<Library> Libraries { get; set; }
    public IndexLogging? Logging { get; set; }
    public string MainClass { get; set; }
    public string MinecraftArguments { get; set; }
    public uint MinimumLauncherVersion { get; set; }
    public DateTimeOffset ReleaseTime { get; set; }
    public DateTimeOffset Time { get; set; }
    public ReleaseType Type { get; set; }
}