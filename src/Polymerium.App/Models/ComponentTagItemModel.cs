namespace Polymerium.App.Models;

public class ComponentTagItemModel
{
    public ComponentTagItemModel(string name, string version, string identity, string description)
    {
        Name = name;
        Version = version;
        Identity = identity;
        Description = description;
    }

    public string Name { get; set; }
    public string Version { get; set; }
    public string Identity { get; set; }
    public string Description { get; set; }
}
