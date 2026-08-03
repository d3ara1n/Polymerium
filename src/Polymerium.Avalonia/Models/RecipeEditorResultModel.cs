using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class RecipeEditorResultModel(string name, string? description) : ModelBase
{
    public string Name => name;

    public string? Description => description;
}
