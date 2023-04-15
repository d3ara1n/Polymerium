using System;

namespace Polymerium.Core.GameAssets;

public struct WorldSave
{
    public string Name { get; set; }
    public string FolderName { get; set; }
    public long Seed { get; set; }
    public string GameVersion { get; set; }
    public DateTimeOffset LastPlayed { get; set; }
}