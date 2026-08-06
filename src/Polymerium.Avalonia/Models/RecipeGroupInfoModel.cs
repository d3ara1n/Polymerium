namespace Polymerium.Avalonia.Models;

public sealed class RecipeGroupInfoModel(string name) : GroupInfoModelBase
{
    public string Name => name;
}
