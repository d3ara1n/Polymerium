namespace Polymerium.App.Models;

public class InstanceModpackReferenceModel
{
    public InstanceModpackReferenceModel(string name, string id, string version, string versionId, string author)
    {
        Name = name;
        Id = id;
        Version = version;
        VersionId = versionId;
        Author = author;
    }

    public string Name { get; set; }
    public string Id { get; set; }
    public string Version { get; set; }
    public string VersionId { get; set; }
    public string Author { get; set; }
}