namespace Polymerium.App.Models;

public class ModpackExporterModel(string key)
{
    public string Key => key;
    public string SelectedExporterLabel { get; set; } = string.Empty;
    public string NameOverride { get; set; } = string.Empty;
    public string AuthorOverride { get; set; } = string.Empty;
    public string VersionOverride { get; set; } = string.Empty;
}
