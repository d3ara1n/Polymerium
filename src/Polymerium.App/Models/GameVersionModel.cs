using System;
using Humanizer;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class GameVersionModel(string name, string Type, DateTimeOffset releaseTime) : ModelBase
{
    public string Name => name;
    public string TypeRaw => Type;
    public DateTimeOffset ReleaseTimeRaw => releaseTime;

    public string ReleaseTime { get; } = releaseTime.Humanize();
}