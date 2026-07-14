using System;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public sealed class ModpackGroupInfoModel(
    ModpackGroupModel owner,
    string name,
    Uri? thumbnail
) : ModelBase
{
    public ModpackGroupModel Owner => owner;

    public string Name => name;

    public Uri? Thumbnail => thumbnail;
}
