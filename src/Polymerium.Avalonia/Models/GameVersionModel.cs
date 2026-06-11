using System;
using Humanizer;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class GameVersionModel(string name, string type, DateTimeOffset releaseTime) : ModelBase
{
    public string Name => name;
    public string TypeRaw => type;
    public DateTimeOffset ReleaseTimeRaw => releaseTime;

    public string ReleaseTime { get; } = releaseTime.Humanize();
}
