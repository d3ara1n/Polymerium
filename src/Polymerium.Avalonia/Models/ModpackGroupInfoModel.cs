using System;

namespace Polymerium.Avalonia.Models;

public sealed class ModpackGroupInfoModel(string name, Uri? thumbnail) : GroupInfoModelBase
{
    public string Name => name;

    public Uri? Thumbnail => thumbnail;
}
