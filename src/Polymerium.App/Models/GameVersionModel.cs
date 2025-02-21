using Polymerium.App.Facilities;
using System;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public class GameVersionModel(string name, string Type, DateTimeOffset releaseTime) : ModelBase
{
    public string Name => name;
    public string TypeRaw => Type;
    public DateTimeOffset ReleaseTimeRaw => releaseTime;
}