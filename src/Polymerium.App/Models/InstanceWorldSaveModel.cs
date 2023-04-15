using System;
using Polymerium.Core.GameAssets;

namespace Polymerium.App.Models;

public class InstanceWorldSaveModel
{
    public InstanceWorldSaveModel(string iconSource, string name, long seed, string gameVersion,
        DateTimeOffset lastPlayed, WorldSave inner)
    {
        Inner = inner;
        IconSource = iconSource;
        Name = name;
        Seed = seed;
        GameVersion = gameVersion;
        LastPlayed = lastPlayed;
    }

    public WorldSave Inner { get; set; }
    public string IconSource { get; set; }
    public string Name { get; set; }
    public long Seed { get; set; }
    public string GameVersion { get; set; }
    public DateTimeOffset LastPlayed { get; set; }
}