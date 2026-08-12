namespace Polymerium.Avalonia.Models;

public sealed class CollectionGroupInfoModel(string name) : GroupInfoModelBase
{
    public string Name => name;
}
