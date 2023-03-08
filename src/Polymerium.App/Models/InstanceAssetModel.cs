using System;
using Polymerium.Abstractions.Resources;

namespace Polymerium.App.Models;

public class InstanceAssetModel
{
    public InstanceAssetModel(ResourceType type, Uri url, string name, string version, string description)
    {
        Type = type;
        Url = url;
        Name = name;
        Version = version;
        Description = description;
    }

    public ResourceType Type { get; set; }
    public Uri Url { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
}